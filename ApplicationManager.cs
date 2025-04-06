namespace ClearDir
{
    /// <summary>
    /// Manages the overall application flow.
    /// </summary>
    public class ApplicationManager
    {
        private readonly ErrorHandler _errorHandler;
        private readonly Dictionary<CancellationTokenType, CancellationTokenSource> _cancellationTokenSources;
        private readonly RenderLoop _renderLoop;

        /// <summary>
        /// Initializes a new instance of the <see cref="ApplicationManager"/> class.
        /// </summary>
        /// <param name="errorHandler">The error handler to use.</param>
        /// <param name="cancellationTokenSources">The cancellation token sources for managing task cancellation.</param>
        /// <param name="renderLoop">The render loop responsible for rendering tasks.</param>
        public ApplicationManager(ErrorHandler errorHandler,
            Dictionary<CancellationTokenType, CancellationTokenSource> cancellationTokenSources,
            RenderLoop renderLoop)
        {
            _errorHandler = errorHandler ?? throw new ArgumentNullException(nameof(errorHandler));
            _cancellationTokenSources = cancellationTokenSources ?? throw new ArgumentNullException(nameof(cancellationTokenSources));
            _renderLoop = renderLoop ?? throw new ArgumentNullException(nameof(renderLoop));
        }

        /// <summary>
        /// Application logic main entry.
        /// </summary>
        public async Task RunAsync()
        {
            try
            {
                await _renderLoop.StartAsync(_cancellationTokenSources[CancellationTokenType.Flush].Token);
                //var searcher = _serviceContainer.Resolve<DirectorySearcher>();
                //await PerformDirectorySearchAsync(searcher, startDirectory, consolePanelService, cancellationTokenSources[CancellationTokenType.Search].Token);
            }
            catch (Exception ex)
            {
                _errorHandler.Halt("An unexpected error occurred.", ex);
            }
        }
    }
}
