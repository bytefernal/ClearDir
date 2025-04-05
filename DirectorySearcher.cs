namespace ClearDir
{
    public class DirectorySearcher
    {
        private readonly ApplicationManager _appManager;

        public DirectorySearcher(ApplicationManager appManager)
        {
            _appManager = appManager ?? throw new ArgumentNullException(nameof(appManager));
        }

        public async Task<List<string>> SearchDirectoriesAsync(
            string root,
            IProgress<DirectorySearchStatus> progress,
            CancellationToken cancellationToken)
        {
            return await Task.Run(() =>
            {
                var foundDirectories = new List<string>();
                int count = 0;

                void Search(string currentDir)
                {
                    cancellationToken.ThrowIfCancellationRequested();

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