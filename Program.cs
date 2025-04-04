using System;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace ClearDir
{
    class Program
    {
        static ConsoleStatusPanel statusPanel = new ConsoleStatusPanel();

        static async Task Main(string[] args)
        {
            // Ensure the panel appears on a new line.
            if (Console.CursorLeft != 0)
                Console.WriteLine();
            
            statusPanel.Add(PanelLabels.Header, "ClearDir v1.0", 0, 0, 80, TextAlignment.Center);
            statusPanel.Add(PanelLabels.Scanning, "Initializing", 0, 1, 80, TextAlignment.Left);
            statusPanel.Add(PanelLabels.FoundCount, "0", 74, 2, 5, TextAlignment.Right);
            statusPanel.Add(PanelLabels.Result, "[OK]", 0, 2, 75, TextAlignment.Left);
            statusPanel.Initialize();

            // Create the cancellation token sources used for the search and flush operations.
            var cancellationTokenSource = new CancellationTokenSource();
            var flushCts = new CancellationTokenSource();

            // Set-up a handler so that when Ctrl+C is pressed, we cancel our operations gracefully.
            Console.CancelKeyPress += (sender, e) =>
            {
                // Prevent the process from terminating immediately.
                e.Cancel = true;
                cancellationTokenSource.Cancel();
                flushCts.Cancel();
            };

            // If no parameter is provided, report usage and exit.
            if (args.Length == 0)
            {
                statusPanel.Update(PanelLabels.Result, "Usage: ClearDir [start-directory]");
                statusPanel.Detach();
                return;
            }

            // Normalize the provided directory path.
            string startDirectory = args[0]
                .Replace('/', Path.DirectorySeparatorChar)
                .Replace('\\', Path.DirectorySeparatorChar);

            if (!Directory.Exists(startDirectory))
            {
                statusPanel.Update(PanelLabels.Result, $"The provided directory does not exist: {startDirectory}");
                statusPanel.Detach();
                return;
            }

            var searcher = new DirectorySearcher();

            // Create an update queue that keeps only the latest update for each element.
            var updateQueue = new PanelUpdateQueue();

            // Launch a background task that flushes updates from the queue to the panel periodically.
            var flushTask = Task.Run(async () =>
            {
                while (!flushCts.Token.IsCancellationRequested)
                {
                    updateQueue.Flush(statusPanel);
                    try
                    {
                        await Task.Delay(100, flushCts.Token);
                    }
                    catch (TaskCanceledException)
                    {
                        break;
                    }
                }
            }, flushCts.Token);

            // Forward progress updates to the update queue asynchronously.
            var progress = new Progress<DirectorySearchStatus>(status =>
            {
                updateQueue.Enqueue(PanelLabels.Scanning, status.CurrentDirectory);
                updateQueue.Enqueue(PanelLabels.FoundCount, status.DirectoryCount.ToString());
            });

            try
            {
                // Run the directory search asynchronously.
                var results = await searcher.SearchDirectoriesAsync(startDirectory, progress, cancellationTokenSource.Token);

                // Flush any pending updates.
                updateQueue.Flush(statusPanel);

                statusPanel.Update(PanelLabels.Result, $"Search completed. Total directories found: {results.Count}");
            }
            catch (OperationCanceledException ex)
            {
                updateQueue.Flush(statusPanel);
                statusPanel.Update(PanelLabels.Result, "Error");
                statusPanel.Detach();
                Console.WriteLine(ex.InnerException?.Message);
            }
            finally
            {
                // Cancel the flush task and wait for it to exit.
                flushCts.Cancel();
                await flushTask;
                
            }
        }
    }
}
