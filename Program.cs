using System;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace ClearDir
{
    class Program
    {
        static ConsoleStatusPanel statusPanel = new();
        static Dictionary<CancellationTokenType, CancellationTokenSource> cancellationTokenSources = InitializeCancellationTokenSources();

        static async Task Main(string[] args)
        {
            InitializeStatusPanel();

            if (!ValidateArgs(args)) return;

            string startDirectory = NormalizePath(args[0]);
            if (!CheckDirectoryExists(startDirectory)) return;

            var searcher = new DirectorySearcher();
            var updateQueue = new PanelUpdateQueue();

            _ = StartFlushTaskAsync(updateQueue);
            await PerformDirectorySearchAsync(searcher, startDirectory, updateQueue);
        }

        static void InitializeStatusPanel()
        {
            statusPanel.Add(PanelLabels.Header, "ClearDir v1.0", 0, 0, 80, TextAlignment.Center);
            statusPanel.Add(PanelLabels.Scanning, "Initializing", 0, 1, 80, TextAlignment.Left);
            statusPanel.Add(PanelLabels.FoundCount, "0", 74, 2, 5, TextAlignment.Right);
            statusPanel.Add(PanelLabels.Result, "[OK]", 0, 2, 75, TextAlignment.Left);
            statusPanel.Initialize();
        }

        static Dictionary<CancellationTokenType, CancellationTokenSource> InitializeCancellationTokenSources()
        {
            var result = new Dictionary<CancellationTokenType, CancellationTokenSource>();

            foreach (CancellationTokenType type in Enum.GetValues(typeof(CancellationTokenType)))
            {
                result[type] = new CancellationTokenSource();
            }

            return result;
        }

        static bool ValidateArgs(string[] args)
        {
            if (args.Length == 0)
            {
                statusPanel.Update(PanelLabels.Result, "Usage: ClearDir [start-directory]");
                return false;
            }
            return true;
        }

        static string NormalizePath(string input)
        {
            return input.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);
        }

        static bool CheckDirectoryExists(string startDirectory)
        {
            if (!Directory.Exists(startDirectory))
            {
                statusPanel.Update(PanelLabels.Result, $"The provided directory does not exist: {startDirectory}");
                return false;
            }
            return true;
        }

        static Task StartFlushTaskAsync(PanelUpdateQueue updateQueue)
        {
            var flushCts = cancellationTokenSources[CancellationTokenType.Flush];
            
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

            return flushTask;
        }

        static async Task PerformDirectorySearchAsync(DirectorySearcher searcher, string startDirectory, PanelUpdateQueue updateQueue)
        {
            var searchCts = cancellationTokenSources[CancellationTokenType.Search];

            var progress = new Progress<DirectorySearchStatus>(status =>
            {
                updateQueue.Enqueue(PanelLabels.Scanning, status.CurrentDirectory);
                updateQueue.Enqueue(PanelLabels.FoundCount, status.DirectoryCount.ToString());
            });

            try
            {
                var results = await searcher.SearchDirectoriesAsync(startDirectory, progress, searchCts.Token);
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
        }

        static void OnProcessExit(object sender, EventArgs e)
        {
            statusPanel.Detach();
            foreach (var cts in cancellationTokenSources.Values)
            {
                cts.Cancel(); // Ensure graceful cleanup
            }
        }
    }
}
