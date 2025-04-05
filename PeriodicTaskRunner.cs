using System;
using System.Threading;
using System.Threading.Tasks;

namespace ClearDir
{
    public class PeriodicTaskRunner
    {
        private readonly Func<Task> _taskToRun;
        private readonly int _intervalMilliseconds;
        private readonly ILogger _logger;

        public PeriodicTaskRunner(Func<Task> taskToRun, int intervalMilliseconds, ILogger logger)
        {
            _taskToRun = taskToRun ?? throw new ArgumentNullException(nameof(taskToRun));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            if (intervalMilliseconds <= 0) throw new ArgumentOutOfRangeException(nameof(intervalMilliseconds));
            _intervalMilliseconds = intervalMilliseconds;
        }

        /// <summary>
        /// Runs the periodic task using the provided cancellation token.
        /// </summary>
        public Task StartAsync(CancellationToken token)
        {
            return Task.Run(async () =>
            {
                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        await _taskToRun();
                        await Task.Delay(_intervalMilliseconds, token);
                    }
                    catch (TaskCanceledException)
                    {
                        _logger.LogInfo("Periodic task was canceled.");
                        break;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Error in periodic task: {ex.Message}", ex);
                    }
                }
            }, token);
        }
    }
}