namespace ClearDir
{
    /// <summary>
    /// Responsible for handling application errors and cleanup.
    /// </summary>
    public class ErrorHandler
    {
        private readonly ILogger _logger;
        private readonly Dictionary<CancellationTokenType, CancellationTokenSource> _cancellationTokenSources;

        /// <summary>
        /// Initializes a new instance of the <see cref="ErrorHandler"/> class.
        /// </summary>
        /// <param name="logger">The logger used to report errors.</param>
        /// <param name="cancellationTokenSources">The cancellation tokens for cleanup.</param>
        public ErrorHandler(ILogger logger, Dictionary<CancellationTokenType, CancellationTokenSource> cancellationTokenSources)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _cancellationTokenSources = cancellationTokenSources ?? throw new ArgumentNullException(nameof(cancellationTokenSources));
        }

        /// <summary>
        /// Handles application cleanup and halts execution.
        /// </summary>
        /// <param name="message">Error message to log.</param>
        /// <param name="exception">Optional exception to include in the log.</param>
        public void Halt(string message, Exception? exception = null)
        {
            foreach (var cts in _cancellationTokenSources.Values)
            {
                cts.Cancel(); // Cleanup
            }

            _logger.LogError(message, exception);
            Environment.Exit(1);
        }
    }
}
