using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace ClearDir
{
    public class DirectorySearchStatus
    {
        public required string CurrentDirectory { get; set; }
        public int DirectoryCount { get; set; }
    }

    public class DirectorySearcher
    {
        /// <summary>
        /// Recursively searches for directories starting from a given root.
        /// Reports progress by updating the current scanning directory and the running count.
        /// If any error (other than cancellation) occurs, a generic error is logged and the search is canceled.
        /// </summary>
        /// <param name="root">The starting directory.</param>
        /// <param name="progress">A progress reporter accepting DirectorySearchStatus updates.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>A list of found directory paths.</returns>
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

                    // Report the current directory and the current count.
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

                            // Report progress for the found directory.
                            progress?.Report(new DirectorySearchStatus
                            {
                                CurrentDirectory = dir,
                                DirectoryCount = count
                            });

                            // Continue searching recursively.
                            Search(dir);
                        }
                    }
                    catch (Exception ex) when (!(ex is OperationCanceledException))
                    {
                        // Log a generic message and trigger cancellation.
                        //Console.Error.WriteLine($"An error occurred while accessing '{currentDir}'. Cancelling search.");
                        throw new OperationCanceledException("Search cancelled due to an error.", ex);
                    }
                }
                
                Search(root);
                return foundDirectories;
            }, cancellationToken);
        }
    }
}
