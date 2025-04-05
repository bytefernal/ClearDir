using System;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace ClearDir
{
    class Program
    {
        private static readonly ConsoleStatusPanel _statusPanel = new();
        private static readonly ILogger _logger = new StatusPanelLogger(_statusPanel);
        private static readonly Dictionary<CancellationTokenType, CancellationTokenSource> _cancellationTokenSources = InitializeCancellationTokenSources();

        static async Task Main(string[] args)
        {
            InitializeStatusPanel();

            if (!ValidateArgs(args)) return;

            string startDirectory = NormalizePath(args[0]);
            if (!CheckDirectoryExists(startDirectory)) return;

            var searcher = new DirectorySearcher(_logger);
            var updateQueue = new PanelUpdateQueue();

            _ = StartFlushTaskAsync(updateQueue);
            await PerformDirectorySearchAsync(searcher, startDirectory, updateQueue);
        }

        /// <summary>
        /// Initializes the status panel with predefined labels and dimensions.
        /// </summary>
        private static void InitializeStatusPanel()
        {
            _statusPanel.Add(PanelLabels.Header, "ClearDir v1.0", 0, 0, 80, TextAlignment.Center);
            _statusPanel.Add(PanelLabels.Scanning, "", 0, 1, 80, TextAlignment.Left);
            _statusPanel.Add(PanelLabels.FoundCount, "", 74, 2, 5, TextAlignment.Right);
            _statusPanel.Add(PanelLabels.Result, "Initializing", 0, 2, 75, TextAlignment.Left);
            _statusPanel.Initialize();
        }

        /// <summary>
        /// Creates and initializes cancellation token sources based on the defined enum values.
        /// </summary>
        private static Dictionary<CancellationTokenType, CancellationTokenSource> InitializeCancellationTokenSources()
        {
            var result = new Dictionary<CancellationTokenType, CancellationTokenSource>();

            foreach (CancellationTokenType type in Enum.GetValues(typeof(CancellationTokenType)))
            {
                result[type] = new CancellationTokenSource();
            }

            return result;
        }

        /// <summary>
        /// Validates the program arguments and ensures proper usage is displayed if arguments are missing.
        /// </summary>
        private static bool ValidateArgs(string[] args)
        {
            if (args.Length == 0)
            {
                _statusPanel.Update(PanelLabels.Result, "Usage: ClearDir [start-directory]");
                return false;
            }
            return true;
        }

        /// <summary>
        /// Normalizes the input directory path to use the proper directory separator characters.
        /// </summary>
        private static string NormalizePath(string input)
        {
            return input.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);
        }

        /// <summary>
        /// Checks whether the specified directory exists, updating the panel if not.
        /// </summary>
        private static bool CheckDirectoryExists(string startDirectory)
        {
            if (!Directory.Exists(startDirectory))
            {
                _statusPanel.Update(PanelLabels.Result, $"The provided directory does not exist: {startDirectory}");
                return false;
            }
            return true;
        }

        /// <summary>
        /// Starts a flush task asynchronously, using a periodic task runner for updates.
        /// </summary>
        private static Task StartFlushTaskAsync(PanelUpdateQueue updateQueue)
        {
            var flushCts = _cancellationTokenSources[CancellationTokenType.Flush];

            Func<Task> flushTask = async () =>
            {
                updateQueue.Flush(_statusPanel);
                await Task.CompletedTask; // Simulate async work if needed
            };

            var periodicTaskRunner = new PeriodicTaskRunner(flushTask, 100, _logger);
            return periodicTaskRunner.StartAsync(flushCts.Token);
        }

        /// <summary>
        /// Performs the directory search asynchronously, updating the panel with progress.
        /// </summary>
        private static async Task PerformDirectorySearchAsync(DirectorySearcher searcher, string startDirectory, PanelUpdateQueue updateQueue)
        {
            var searchCts = _cancellationTokenSources[CancellationTokenType.Search];

            var progress = new Progress<DirectorySearchStatus>(status =>
            {
                updateQueue.Enqueue(PanelLabels.Scanning, status.CurrentDirectory);
                updateQueue.Enqueue(PanelLabels.FoundCount, status.DirectoryCount.ToString());
                updateQueue.Enqueue(PanelLabels.Result, "Searching");
            });

            try
            {
                var results = await searcher.SearchDirectoriesAsync(startDirectory, progress, searchCts.Token);
                updateQueue.Flush(_statusPanel);
                _statusPanel.Update(PanelLabels.Result, $"Search completed.");
            }
            catch (OperationCanceledException ex)
            {
                updateQueue.Flush(_statusPanel);
                _statusPanel.Update(PanelLabels.Result, "Error");
                _statusPanel.Detach();
                Console.WriteLine(ex.InnerException?.Message);
            }
        }

        /// <summary>
        /// Cleans up resources and cancels ongoing operations when the application exits.
        /// </summary>
        private static void OnProcessExit(object sender, EventArgs e)
        {
            foreach (var cts in _cancellationTokenSources.Values)
            {
                cts.Cancel(); // Ensure graceful cleanup
            }
            _statusPanel.Detach();
        }
    }
}
