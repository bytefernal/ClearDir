namespace ClearDir
{
    public class DirectorySearcher
    {
        private readonly ApplicationManager _appManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="DirectorySearcher"/> class.
        /// </summary>
        /// <param name="appManager">The application manager for handling critical operations like halting the application.</param>
        public DirectorySearcher(ApplicationManager appManager)
        {
            _appManager = appManager ?? throw new ArgumentNullException(nameof(appManager));
        }

        /// <summary>
        /// Recursively searches for directories starting from the specified root directory.
        /// Reports progress and halts the application in case of critical errors.
        /// </summary>
        /// <param name="root">The root directory to start the search.</param>
        /// <param name="progress">The progress reporter to provide updates on the current status.</param>
        /// <param name="cancellationToken">The token to observe for cancellation requests.</param>
        /// <returns>A list of found directory paths.</returns>
        public async Task<List<string>> SearchDirectoriesAsync(
            string root,
            IProgress<DirectorySearchStatus> progress,
            CancellationToken cancellationToken)
        {
            // Validate parameters
            if (string.IsNullOrWhiteSpace(root))
                throw new ArgumentException("Root directory cannot be null or empty.", nameof(root));

            if (!Directory.Exists(root))
                throw new DirectoryNotFoundException($"The directory '{root}' does not exist.");

            return await Task.Run(() =>
            {
                var foundDirectories = new List<string>();
                int count = 0;

                void Search(string currentDir)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    // Report the current directory and count progress
                    progress?.Report(new DirectorySearchStatus
                    {
                        CurrentDirectory = currentDir,
                        DirectoryCount = count
                    });

                    try
                    {
                        foreach (var dir in Directory.EnumerateDirectories(currentDir))
                        {
                            cancellationToken.ThrowIfCancellationRequested();

                            foundDirectories.Add(dir);
                            count++;

                            progress?.Report(new DirectorySearchStatus
                            {
                                CurrentDirectory = dir,
                                DirectoryCount = count
                            });

                            // Recursive call for subdirectories
                            Search(dir);
                        }
                    }
                    catch (Exception ex) when (!(ex is OperationCanceledException))
                    {
                        _appManager.HaltApplication($"An error occurred while accessing '{currentDir}'.", ex);
                    }
                }

                Search(root);
                return foundDirectories;
            }, cancellationToken);
        }
    }
}
