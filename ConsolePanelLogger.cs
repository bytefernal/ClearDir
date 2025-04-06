namespace ClearDir
{
    /// <summary>
    /// Logs messages to a console panel and handles updates.
    /// </summary>
    public class ConsolePanelLogger : ILogger
    {
        private readonly ConsolePanelService _consolePanelService;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConsolePanelLogger"/> class.
        /// </summary>
        /// <param name="consolePanelService">The service managing the console panel.</param>
        public ConsolePanelLogger(ConsolePanelService consolePanelService)
        {
            _consolePanelService = consolePanelService ?? throw new ArgumentNullException(nameof(consolePanelService));
        }

        /// <summary>
        /// Logs informational messages to the console panel.
        /// </summary>
        /// <param name="message">The message to log.</param>
        public void LogInfo(string message)
        {
            _consolePanelService.Enqueue(PanelLabels.Result, message);
        }

        /// <summary>
        /// Logs error messages to the console panel, detaches the panel, 
        /// and optionally includes exception details.
        /// </summary>
        /// <param name="message">The error message to log.</param>
        /// <param name="exception">Optional exception details to include in the log.</param>
        public void LogError(string message, Exception? exception = null)
        {
            Console.WriteLine($"ERROR (ConsolePanel): {message}");
            if (exception != null)
            {
                Console.WriteLine($"Exception Details: {exception}");
            }
        }
    }
}
