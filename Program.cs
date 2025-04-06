namespace ClearDir
{
    class Program
    {
        private static ServiceContainer _serviceContainer = new();

        static async Task Main(string[] args)
        {
            ConfigureServices();

            var cancellationTokenSources = _serviceContainer.Resolve<Dictionary<CancellationTokenType, CancellationTokenSource>>();
            var consolePanelService = _serviceContainer.Resolve<ConsolePanelService>();
            var periodicFlushManager = _serviceContainer.Resolve<RenderLoop>();

            if (!ValidateArgs(args, consolePanelService)) return;

            string startDirectory = args[0];
            if (!CheckDirectoryExists(startDirectory, consolePanelService)) return;

            var _ = periodicFlushManager.StartAsync(cancellationTokenSources[CancellationTokenType.Flush].Token);
            var searcher = _serviceContainer.Resolve<DirectorySearcher>();
            await PerformDirectorySearchAsync(searcher, startDirectory, consolePanelService, cancellationTokenSources[CancellationTokenType.Search].Token);
        }

        /// <summary>
        /// Configures and registers services in the DI container.
        /// </summary>
        private static void ConfigureServices()
        {
            _serviceContainer.Register(() =>
            {
                var consolePanel = new ConsolePanel();
                consolePanel.Add(PanelLabels.Header, "ClearDir v1.0", 0, 0, 80, TextAlignment.Center);
                consolePanel.Add(PanelLabels.Scanning, "", 0, 1, 80, TextAlignment.Left);
                consolePanel.Add(PanelLabels.FoundCount, "", 74, 2, 5, TextAlignment.Right);
                consolePanel.Add(PanelLabels.Result, "Initializing", 0, 2, 75, TextAlignment.Left);
                consolePanel.Initialize();

                return new ConsolePanelService(consolePanel);
            });

            _serviceContainer.Register(() =>
            {
                var cancellationTokenSources = new Dictionary<CancellationTokenType, CancellationTokenSource>();
                foreach (CancellationTokenType type in Enum.GetValues(typeof(CancellationTokenType)))
                {
                    cancellationTokenSources[type] = new CancellationTokenSource();
                }
                return cancellationTokenSources;
            });

            _serviceContainer.Register(() =>
            {
                var logger = _serviceContainer.Resolve<ILogger>();
                var cancellationTokenSources = _serviceContainer.Resolve<Dictionary<CancellationTokenType, CancellationTokenSource>>();
                return new ApplicationManager(logger, cancellationTokenSources);
            });

            _serviceContainer.Register(() =>
            {
                var appManager = _serviceContainer.Resolve<ApplicationManager>();
                
                return new DirectorySearcher(appManager);
            });

            _serviceContainer.Register(() =>
            {
                var consolePanelService = _serviceContainer.Resolve<ConsolePanelService>();
                var logger = _serviceContainer.Resolve<ILogger>();
                return new RenderLoop(consolePanelService, logger, 100);
            });
        }

        private static bool ValidateArgs(string[] args, ConsolePanelService consolePanelService)
        {
            if (args.Length == 0)
            {
                consolePanelService.Enqueue(PanelLabels.Result, "Usage: ClearDir [start-directory]");
                consolePanelService.Flush();
                return false;
            }
            return true;
        }

        private static bool CheckDirectoryExists(string startDirectory, ConsolePanelService consolePanelService)
        {
            if (!Directory.Exists(startDirectory))
            {
                consolePanelService.Enqueue(PanelLabels.Result, $"The provided directory does not exist: {startDirectory}");
                consolePanelService.Flush();
                return false;
            }
            return true;
        }

        private static async Task PerformDirectorySearchAsync(
            DirectorySearcher searcher,
            string startDirectory,
            ConsolePanelService consolePanelService,
            CancellationToken cancellationToken)
        {
            // Use progress to update the ConsolePanelService directly
            var progress = new Progress<DirectorySearchStatus>(status =>
            {
                consolePanelService.Enqueue(PanelLabels.Scanning, status.CurrentDirectory);
                consolePanelService.Enqueue(PanelLabels.FoundCount, status.DirectoryCount.ToString());
                consolePanelService.Enqueue(PanelLabels.Result, "Searching");
            });

            try
            {
                // Perform the search
                var results = await searcher.SearchDirectoriesAsync(startDirectory, progress, cancellationToken);

                // Report completion
                consolePanelService.Enqueue(PanelLabels.Result, $"Done. Found {results.Count} directories.");
                consolePanelService.Flush();
            }
            catch (OperationCanceledException)
            {
                // Handle cancellation gracefully
                consolePanelService.Enqueue(PanelLabels.Result, "Search canceled.");
                consolePanelService.Flush();
            }
            catch (Exception ex)
            {
                // Log critical errors
                var logger = _serviceContainer.Resolve<ILogger>();
                logger.LogError($"Critical error during search: {ex.Message}", ex);
            }
        }


        // private static async Task PerformDirectorySearchAsync(
        //     DirectorySearcher searcher,
        //     string startDirectory,
        //     ConsolePanelService ConsolePanelService)
        // {
        //     var searchCts = _serviceContainer.Resolve<Dictionary<CancellationTokenType, CancellationTokenSource>>()
        //                      [CancellationTokenType.Search];

        //     var progress = new Progress<DirectorySearchStatus>(status =>
        //     {
        //         ConsolePanelService.Enqueue(PanelLabels.Scanning, status.CurrentDirectory);
        //         ConsolePanelService.Enqueue(PanelLabels.FoundCount, status.DirectoryCount.ToString());
        //         ConsolePanelService.Enqueue(PanelLabels.Result, "Searching");
        //     });

        //     var results = await searcher.SearchDirectoriesAsync(startDirectory, progress, searchCts.Token);
        //     ConsolePanelService.Enqueue(PanelLabels.Result, "Done");
        //     ConsolePanelService.Flush();
        // }
    }
}
