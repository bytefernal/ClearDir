namespace ClearDir
{
    /// <summary>
    /// Manages updates to the status panel using a queue mechanism.
    /// Ensures that the panel is updated in a structured and thread-safe manner.
    /// </summary>
    public class StatusPanelManager
    {
        private readonly ConsoleStatusPanel _statusPanel;
        private readonly PanelUpdateQueue _updateQueue;

        public StatusPanelManager(ConsoleStatusPanel statusPanel)
        {
            _statusPanel = statusPanel ?? throw new ArgumentNullException(nameof(statusPanel));
            _updateQueue = new PanelUpdateQueue();
        }

        /// <summary>
        /// Initializes the status panel with predefined labels and dimensions.
        /// </summary>
        public void Initialize()
        {
            _statusPanel.Add(PanelLabels.Header, "ClearDir v1.0", 0, 0, 80, TextAlignment.Center);
            _statusPanel.Add(PanelLabels.Scanning, "", 0, 1, 80, TextAlignment.Left);
            _statusPanel.Add(PanelLabels.FoundCount, "", 74, 2, 5, TextAlignment.Right);
            _statusPanel.Add(PanelLabels.Result, "Initializing", 0, 2, 75, TextAlignment.Left);
            _statusPanel.Initialize();
        }

        /// <summary>
        /// Enqueues an update for the panel element with the specified label.
        /// </summary>
        public void EnqueueUpdate(PanelLabels label, string text)
        {
            _updateQueue.Enqueue(label, text);
        }

        /// <summary>
        /// Flushes all updates to the status panel, applying them in batch.
        /// </summary>
        public void Flush()
        {
            _updateQueue.Flush(_statusPanel);
        }

        /// <summary>
        /// Detaches the status panel gracefully.
        /// </summary>
        public void Detach()
        {
            _statusPanel.Detach();
        }
    }
}
