using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ClearDir
{
    /// <summary>
    /// Handles the execution of directory searches with progress reporting and retrieves the results.
    /// </summary>
    public class DirectorySearchService
    {
        private readonly DirectorySearcher _searcher;
        private readonly ConsolePanelService _consolePanelService;
        private readonly Dictionary<CancellationTokenType, CancellationTokenSource> _cancellationTokenSources;
        private readonly string _startDirectory;

        /// <summary>
        /// Initializes a new instance of the <see cref="DirectorySearchService"/> class.
        /// </summary>
        /// <param name="searcher">The directory searcher to use.</param>
        /// <param name="consolePanelService">The console panel service for logging updates.</param>
        /// <param name="cancellationTokenSources">The cancellation tokens for managing search execution.</param>
        /// <param name="startDirectory">The directory to start the search from.</param>
        public DirectorySearchService(
            DirectorySearcher searcher,
            ConsolePanelService consolePanelService,
            Dictionary<CancellationTokenType, CancellationTokenSource> cancellationTokenSources,
            string startDirectory)
        {
            _searcher = searcher ?? throw new ArgumentNullException(nameof(searcher));
            _consolePanelService = consolePanelService ?? throw new ArgumentNullException(nameof(consolePanelService));
            _cancellationTokenSources = cancellationTokenSources ?? throw new ArgumentNullException(nameof(cancellationTokenSources));
            _startDirectory = startDirectory ?? throw new ArgumentNullException(nameof(startDirectory));
        }

        /// <summary>
        /// Runs the directory search asynchronously with progress reporting and retrieves found directories.
        /// </summary>
        /// <returns>A list of found directories.</returns>
        public async Task<List<string>> RunAsync()
        {
            var searchCts = _cancellationTokenSources[CancellationTokenType.Search];

            var progress = new Progress<DirectorySearchStatus>(status =>
            {
                _consolePanelService.Enqueue(PanelLabels.Scanning, status.CurrentDirectory);
                _consolePanelService.Enqueue(PanelLabels.FoundCount, status.DirectoryCount.ToString());
                _consolePanelService.Enqueue(PanelLabels.Result, "Searching");
            });

            var foundDirectories = await _searcher.SearchDirectoriesAsync(_startDirectory, progress, searchCts.Token);

            // Update the console panel when search is complete
            _consolePanelService.Enqueue(PanelLabels.Result, "Done");
            _consolePanelService.Flush();

            return foundDirectories;
        }
    }
}
