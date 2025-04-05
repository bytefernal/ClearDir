namespace ClearDir
{
    /// <summary>
    /// Logs messages to a status panel for informational updates 
    /// and to the console for error reporting.
    /// </summary>
    public class StatusPanelLogger : ILogger
    {
        private readonly StatusPanelManager _statusPanelManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="StatusPanelLogger"/> class.
        /// </summary>
        /// <param name="statusPanelManager">The manager handling the status panel.</param>
        /// <exception cref="ArgumentNullException">Thrown if the provided <paramref name="statusPanelManager"/> is null.</exception>
        public StatusPanelLogger(StatusPanelManager statusPanelManager)
        {
            _statusPanelManager = statusPanelManager ?? throw new ArgumentNullException(nameof(statusPanelManager));
        }

        /// <summary>
        /// Logs informational messages by enqueuing them to the status panel manager.
        /// </summary>
        /// <param name="message">The message to log.</param>
        public void LogInfo(string message)
        {
            _statusPanelManager.EnqueueUpdate(PanelLabels.Result, message);
            _statusPanelManager.Flush();
        }

        /// <summary>
        /// Logs error messages to the console, detaches the status panel, 
        /// and optionally includes exception details.
        /// </summary>
        /// <param name="message">The error message to log.</param>
        /// <param name="exception">Optional exception details to include in the log.</param>
        public void LogError(string message, Exception? exception = null)
        {
            _statusPanelManager.Detach(); // Ensure the status panel is detached on errors
            Console.WriteLine($"ERROR: {message}");
            if (exception != null)
            {
                Console.WriteLine($"Exception Details: {exception}");
            }
        }
    }
}
