namespace ClearDir
{
    /// <summary>
    /// Logs messages to a status panel for informational updates 
    /// and to the console for error reporting. Handles detaching the status panel on errors.
    /// </summary>
    public class StatusPanelLogger : ILogger
    {
        private readonly ConsoleStatusPanel _statusPanel;

        /// <summary>
        /// Initializes a new instance of the <see cref="StatusPanelLogger"/> class.
        /// </summary>
        /// <param name="statusPanel">The status panel to be used for logging information.</param>
        /// <exception cref="ArgumentNullException">Thrown if the provided <paramref name="statusPanel"/> is null.</exception>
        public StatusPanelLogger(ConsoleStatusPanel statusPanel)
        {
            _statusPanel = statusPanel ?? throw new ArgumentNullException(nameof(statusPanel));
        }

        /// <summary>
        /// Logs informational messages to the status panel.
        /// </summary>
        /// <param name="message">The message to log.</param>
        public void LogInfo(string message)
        {
            _statusPanel.Update(PanelLabels.Result, message);
        }

        /// <summary>
        /// Logs error messages to the console, detaches the status panel, 
        /// and optionally includes exception details.
        /// </summary>
        /// <param name="message">The error message to log.</param>
        /// <param name="exception">Optional exception details to include in the log.</param>
        public void LogError(string message, Exception? exception = null)
        {
            _statusPanel.Detach(); // Ensure the status panel is detached on errors
            Console.WriteLine($"ERROR: {message}");
            if (exception != null)
            {
                Console.WriteLine($"Exception Details: {exception}");
            }
        }
    }
}
