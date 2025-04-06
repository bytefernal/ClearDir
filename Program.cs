namespace ClearDir
{
    class Program
    {
        private static ServiceContainer _serviceContainer = new();

        static async Task Main(string[] args)
        {
            ConfigureServices();

            var consolePanelService = _serviceContainer.Resolve<ConsolePanelService>();
            var applicationManager = _serviceContainer.Resolve<ApplicationManager>();
            var cancellationTokenSources = new Dictionary<CancellationTokenType, CancellationTokenSource>();

            if (!ValidateArgs(args, consolePanelService)) return;

            string startDirectory = args[0];
            if (!CheckDirectoryExists(startDirectory, consolePanelService)) return;

            await applicationManager.RunAsync();
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

            _serviceContainer.Register<ILogger>(() => 
            {
                var consolePanelService = _serviceContainer.Resolve<ConsolePanelService>();
                return new ConsolePanelLogger(consolePanelService);
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
                return new ErrorHandler(logger, cancellationTokenSources);
            });

            _serviceContainer.Register(() =>
            {
                var consolePanelService = _serviceContainer.Resolve<ConsolePanelService>();
                var logger = _serviceContainer.Resolve<ILogger>();
                return new RenderLoop(consolePanelService, logger, 100);
            });

            _serviceContainer.Register<ApplicationManager>();

            _serviceContainer.Register(() =>
            {
                var appManager = _serviceContainer.Resolve<ApplicationManager>();
                return new DirectorySearcher(appManager);
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
            ConsolePanelService ConsolePanelService)
        {
            var searchCts = _serviceContainer.Resolve<Dictionary<CancellationTokenType, CancellationTokenSource>>()
                             [CancellationTokenType.Search];

            var progress = new Progress<DirectorySearchStatus>(status =>
            {
                ConsolePanelService.Enqueue(PanelLabels.Scanning, status.CurrentDirectory);
                ConsolePanelService.Enqueue(PanelLabels.FoundCount, status.DirectoryCount.ToString());
                ConsolePanelService.Enqueue(PanelLabels.Result, "Searching");
            });

            var results = await searcher.SearchDirectoriesAsync(startDirectory, progress, searchCts.Token);
            ConsolePanelService.Enqueue(PanelLabels.Result, "Done");
            ConsolePanelService.Flush();
        }
    }
}
