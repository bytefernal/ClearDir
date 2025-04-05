namespace ClearDir
{
    class Program
    {
        private static readonly StatusPanelManager _statusPanelManager = new(new ConsoleStatusPanel());
        private static readonly ILogger _logger = new StatusPanelLogger(_statusPanelManager);
        private static readonly ApplicationManager _appManager = new ApplicationManager(_logger);
        private static readonly Dictionary<CancellationTokenType, CancellationTokenSource> _cancellationTokenSources = InitializeCancellationTokenSources();

        static async Task Main(string[] args)
        {
            _statusPanelManager.Initialize();

            if (!ValidateArgs(args)) return;

            string startDirectory = args[0];
            if (!CheckDirectoryExists(startDirectory)) return;

            var searcher = new DirectorySearcher(_appManager);

            _ = StartFlushTaskAsync(_statusPanelManager);
            await PerformDirectorySearchAsync(searcher, startDirectory, _statusPanelManager);
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
                _statusPanelManager.EnqueueUpdate(PanelLabels.Result, "Usage: ClearDir [start-directory]");
                _statusPanelManager.Flush();
                return false;
            }
            return true;
        }

        /// <summary>
        /// Checks whether the specified directory exists, updating the panel if not.
        /// </summary>
        private static bool CheckDirectoryExists(string startDirectory)
        {
            if (!Directory.Exists(startDirectory))
            {
                _statusPanelManager.EnqueueUpdate(PanelLabels.Result, $"The provided directory does not exist: {startDirectory}");
                _statusPanelManager.Flush();
                return false;
            }
            return true;
        }

        /// <summary>
        /// Starts a flush task asynchronously, using a periodic task runner for updates.
        /// </summary>
        private static Task StartFlushTaskAsync(StatusPanelManager statusPanelManager)
        {
            var flushCts = _cancellationTokenSources[CancellationTokenType.Flush];

            Func<Task> flushTask = async () =>
            {
                statusPanelManager.Flush();
                await Task.CompletedTask; // Simulate async work if needed
            };

            var periodicTaskRunner = new PeriodicTaskRunner(flushTask, 100, _logger);
            return periodicTaskRunner.StartAsync(flushCts.Token);
        }

        /// <summary>
        /// Performs the directory search asynchronously, updating the panel with progress.
        /// </summary>
        private static async Task PerformDirectorySearchAsync(DirectorySearcher searcher, string startDirectory, StatusPanelManager statusPanelManager)
        {
            var searchCts = _cancellationTokenSources[CancellationTokenType.Search];

            var progress = new Progress<DirectorySearchStatus>(status =>
            {
                statusPanelManager.EnqueueUpdate(PanelLabels.Scanning, status.CurrentDirectory);
                statusPanelManager.EnqueueUpdate(PanelLabels.FoundCount, status.DirectoryCount.ToString());
                statusPanelManager.EnqueueUpdate(PanelLabels.Result, "Searching");
            });

            var results = await searcher.SearchDirectoriesAsync(startDirectory, progress, searchCts.Token);
            statusPanelManager.EnqueueUpdate(PanelLabels.Result, "Done");
            statusPanelManager.Flush();
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
            _statusPanelManager.Detach();
        }
    }
}
