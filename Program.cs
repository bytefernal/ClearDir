namespace ClearDir
{
    class Program
    {
        private static ServiceContainer _serviceContainer = new();

        static async Task Main(string[] args)
        {
            if (!ValidateArgs(args) || !CheckDirectoryExists(args[0])) return;

            ConfigureServices(args[0]);

            await _serviceContainer.Resolve<ApplicationManager>().RunAsync();
        }

        /// <summary>
        /// Configures and registers services in the DI container.
        /// </summary>
        private static void ConfigureServices(string startDirectory)
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

            _serviceContainer.Register<ILogger, ConsolePanelLogger>();
            _serviceContainer.Register<ErrorHandler>();
            _serviceContainer.Register<DirectorySearcher>();
            _serviceContainer.Register(() =>
            {
                var directorySearcher = _serviceContainer.Resolve<DirectorySearcher>();
                var consolePanelService = _serviceContainer.Resolve<ConsolePanelService>();
                var cancellationTokenSources = _serviceContainer.Resolve<Dictionary<CancellationTokenType, CancellationTokenSource>>();
                return new DirectorySearchService(directorySearcher, consolePanelService, cancellationTokenSources, startDirectory);
            });
            
            _serviceContainer.Register(() =>
            {
                var consolePanelService = _serviceContainer.Resolve<ConsolePanelService>();
                var logger = _serviceContainer.Resolve<ILogger>();
                return new RenderLoop(consolePanelService, logger, 100);
            });

            _serviceContainer.Register<ApplicationManager>();
        }

        private static bool ValidateArgs(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Usage: cleardir [start-directory]");
                return false;
            }
            return true;
        }

        private static bool CheckDirectoryExists(string startDirectory)
        {
            if (!Directory.Exists(startDirectory))
            {
                Console.WriteLine($"The provided directory does not exist: {startDirectory}");
                return false;
            }
            return true;
        }
    }
}
