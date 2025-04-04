using System;
using System.Collections.Generic;

namespace ClearDir
{
    /// <summary>
    /// Queues updates for the ConsoleStatusPanel. Each call to Enqueue stores
    /// the latest update for a specific panel element (by label). When Flush() is called,
    /// the latest update values are applied to the panel.
    /// 
    /// This design ensures that if several updates occur while the panel is slow to refresh,
    /// only the most recent update per element is applied.
    /// </summary>
    public class PanelUpdateQueue
    {
        private readonly object _lock = new object();
        private readonly Dictionary<PanelLabels, string> _updates = new Dictionary<PanelLabels, string>();

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
        /// Flushes the queued updates by applying the latest update for each panel element
        /// to the specified ConsoleStatusPanel, then clears the queue.
        /// </summary>
        /// <param name="panel">The ConsoleStatusPanel to update.</param>
        public void Flush(ConsoleStatusPanel panel)
        {
            Dictionary<PanelLabels, string> updatesToApply;
            lock (_lock)
            {
                // Clone current updates to avoid holding the lock during panel updates.
                updatesToApply = new Dictionary<PanelLabels, string>(_updates);
                // Clear the queue since we're processing these updates.
                _updates.Clear();
            }
            foreach (var update in updatesToApply)
            {
                panel.Update(update.Key, update.Value);
            }
        }
    }
}
