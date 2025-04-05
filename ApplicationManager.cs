namespace ClearDir
{
    /// <summary>
    /// Manages application flow, including halting execution on errors.
    /// </summary>
    public class ApplicationManager
    {
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="ApplicationManager"/> class.
        /// </summary>
        /// <param name="logger">The logger to use for logging errors.</param>
        public ApplicationManager(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Logs an error and halts the application with a specified exit code.
        /// </summary>
        /// <param name="message">The error message to log.</param>
        /// <param name="exception">Optional exception details to log.</param>
        public void HaltApplication(string message, Exception? exception = null)
        {
            _logger.LogError(message, exception);
            Environment.Exit(1); // Exit with error code 1
        }
    }
}
