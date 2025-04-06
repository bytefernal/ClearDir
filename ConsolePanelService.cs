namespace ClearDir
{
    /// <summary>
    /// Manages updates to the status panel using a queue mechanism.
    /// Ensures that the panel is updated in a structured and thread-safe manner.
    /// </summary>
    public class ConsolePanelService
    {
        private readonly ConsolePanel _consolePanel;
        private readonly object _lock = new object();
        public readonly Dictionary<PanelLabels, string> _updates = new Dictionary<PanelLabels, string>();

        public ConsolePanelService(ConsolePanel consolePanel)
        {
            _consolePanel = consolePanel;
        }

        /// <summary>
        /// Enqueues an update for a given panel element. Any previously queued update for that
        /// element is replaced with the new text.
        /// </summary>
        /// <param name="label">The panel element label.</param>
        /// <param name="text">The new text to update.</param>
        public void Enqueue(PanelLabels label, string text)
        {
            lock (_lock)
            {
                _updates[label] = text; // Overwrite with the latest update.
            }
        }

        /// <summary>
        /// Flushes all updates to the status panel, applying them in batch.
        /// </summary>
        public void Flush()
        {
            lock (_lock)
            {
                // Clone current updates to avoid holding the lock during panel updates.
                //var updatesToApply = new Dictionary<PanelLabels, string>(_updates);
                // Clear the queue since we're processing these updates.
                //_updates.Clear();
                _consolePanel.Update(_updates);
            }
        }
    }
}
