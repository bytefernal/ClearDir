using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ClearDir
{
    /// <summary>
    /// Handles periodic flushing of updates for a status panel manager.
    /// This component combines periodic task execution and panel management into a single entity.
    /// </summary>
    public class RenderLoop
    {
        private readonly ConsolePanelService _consolePanelService;
        private readonly ILogger _logger;
        private readonly int _intervalMilliseconds;

        public RenderLoop(ConsolePanelService consolePanelService, ILogger logger, int intervalMilliseconds)
        {
            _consolePanelService = consolePanelService ?? throw new ArgumentNullException(nameof(consolePanelService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            if (intervalMilliseconds <= 0) throw new ArgumentOutOfRangeException(nameof(intervalMilliseconds));
            _intervalMilliseconds = intervalMilliseconds;
        }

        /// <summary>
        /// Starts the periodic flush task asynchronously, managing cancellation and logging.
        /// </summary>
        public Task RunAsync(CancellationToken cancellationToken)
        {
            return Task.Run(async () =>
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        // Perform the flush operation
                        _consolePanelService.Flush();

                        // Wait for the next interval
                        await Task.Delay(_intervalMilliseconds, cancellationToken);
                    }
                    catch (TaskCanceledException)
                    {
                        _logger.LogInfo("Periodic flush task was canceled.");
                        break;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Error during periodic flush: {ex.Message}", ex);
                        break; // Exit the loop if a critical error occurs
                    }
                }
            }, cancellationToken);
        }
    }
}
