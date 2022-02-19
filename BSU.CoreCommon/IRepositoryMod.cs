using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BSU.Hashes;

namespace BSU.CoreCommon
{
    /// <summary>
    /// Single mod within a <see cref="IRepository"/>
    /// </summary>
    public interface IRepositoryMod
    {
        Task<List<string>> GetFileList(CancellationToken cancellationToken); // TODO: make AsyncEnumerable

        /// <summary>
        /// Returns metadata for a file. Exception if file data not found.
        /// </summary>
        /// <param name="path">Relative path. Using forward slashes, starting with a forward slash, and in lower case.</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<FileHash> GetFileHash(string path, CancellationToken cancellationToken);

        /// <summary>
        /// Downloads a single file. Exception if file is not found.
        /// </summary>
        /// <param name="path">Relative path. Using forward slashes, starting with a forward slash, and in lower case.</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<byte[]> GetFile(string path, CancellationToken cancellationToken);

        /// <summary>
        /// Attempts to build or extract a display name for this mod.
        /// </summary>
        /// <returns></returns>
        Task<(string name, string version)> GetDisplayInfo(CancellationToken cancellationToken);

        /// <summary>
        /// Returns metadata for a file. Exception if file data not found.
        /// Looks up stored value, no IO overhead.
        /// </summary>
        /// <param name="path">Relative path. Using forward slashes, starting with a forward slash, and in lower case.</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<ulong> GetFileSize(string path, CancellationToken cancellationToken);

        /// <summary>
        /// Downloads a file. Exception if not found.
        /// </summary>
        /// <param name="path">Relative path. Using forward slashes, starting with a forward slash, and in lower case.</param>
        /// <param name="progress">Called occasionally with number of bytes downloaded since last call</param>
        /// <param name="cancellationToken">Can be used to cancel this operation.</param>
        /// <param name="fileSystem"></param>
        Task DownloadTo(string path, IFileSystem fileSystem, IProgress<ulong> progress, CancellationToken cancellationToken);

        /// <summary>
        /// Updates an existing file. Exception if not found.
        /// </summary>
        /// <param name="path">Relative path. Using forward slashes, starting with a forward slash, and in lower case.</param>
        /// <param name="progress">Called occasionally with number of bytes downloaded since last call</param>
        /// <param name="cancellationToken">Can be used to cancel this operation.</param>
        /// <param name="fileSystem"></param>
        Task UpdateTo(string path, IFileSystem fileSystem, IProgress<ulong> progress, CancellationToken cancellationToken);
    }
}
