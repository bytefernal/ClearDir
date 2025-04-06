namespace ClearDir
{
    /// <summary>
    /// Manages application flow, including halting execution on errors.
    /// </summary>
    public class ApplicationManager
    {
        private bool _isFinalized = false;
        private readonly ILogger _logger;
        private readonly Dictionary<CancellationTokenType, CancellationTokenSource> _cancellationTokenSources;

        /// <summary>
        /// Initializes a new instance of the <see cref="ApplicationManager"/> class.
        /// </summary>
        /// <param name="logger">The logger to use for logging errors.</param>
        public ApplicationManager(ILogger logger, 
            Dictionary<CancellationTokenType, CancellationTokenSource> cancellationTokenSources)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _cancellationTokenSources = cancellationTokenSources ?? throw new ArgumentNullException(nameof(cancellationTokenSources));
        }

        /// <summary>
        /// Logs an error and halts the application with a specified exit code.
        /// </summary>
        /// <param name="message">The error message to log.</param>
        /// <param name="exception">Optional exception details to log.</param>
        public void HaltApplication(string message, Exception? exception = null)
        {
            if (_isFinalized) return;

            _isFinalized = true;

            foreach (var cts in _cancellationTokenSources.Values)
            {
                cts.Cancel(); // Ensure graceful cleanup
            }

            if (!string.IsNullOrEmpty(message))
            {
                _logger.LogError(message, exception);
                Environment.Exit(1); // Exit with error code 1
            }
            else
            {
                Environment.Exit(0);
            }
        }
    }
}
