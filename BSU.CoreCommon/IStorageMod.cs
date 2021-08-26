using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using BSU.Hashes;

namespace BSU.CoreCommon
{
    public interface IStorageMod
    {
        /// <summary>
        /// Attempts to retrieve a display name for this mod folder.
        /// </summary>
        /// <returns></returns>
        Task<string> GetDisplayName(CancellationToken cancellationToken);
        Task<List<string>> GetFileList(CancellationToken cancellationToken);

        /// <summary>
        /// Returns a read-only file stream. Must be disposed.
        /// </summary>
        /// <param name="path">Relative path. Using forward slashes, starting with a forward slash, and in lower case.</param>
        /// <returns></returns>
        Task<Stream> OpenFile(string path, FileAccess access, CancellationToken cancellationToken);

        /// <summary>
        /// Get hash of a local file. Null if it doesn't exist.
        /// </summary>
        /// <param name="path">Relative path. Using forward slashes, starting with a forward slash, and in lower case.</param>
        /// <returns></returns>
        Task<FileHash> GetFileHash(string path, CancellationToken cancellationToken);

        /// <summary>
        /// Deletes a file. Exception if it doesn't exists.
        /// </summary>
        /// <param name="path">Relative path. Using forward slashes, starting with a forward slash, and in lower case.</param>
        Task DeleteFile(string path, CancellationToken cancellationToken);
    }
}
