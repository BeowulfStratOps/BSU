using System;
using System.Collections.Generic;
using System.Threading;
using BSU.Hashes;

namespace BSU.CoreCommon
{
    /// <summary>
    /// Single mod within a <see cref="IRepository"/>
    /// </summary>
    public interface IRepositoryMod
    {
        List<string> GetFileList();

        /// <summary>
        /// Returns metadata for a file. Null if file data not found.
        /// Looks up stored value, no IO overhead.
        /// </summary>
        /// <param name="path">Relative path. Using forward slashes, starting with a forward slash, and in lower case.</param>
        /// <returns></returns>
        FileHash GetFileHash(string path);

        /// <summary>
        /// Downloads a single file. Exception if file is not found.
        /// </summary>
        /// <param name="path">Relative path. Using forward slashes, starting with a forward slash, and in lower case.</param>
        /// <returns></returns>
        byte[] GetFile(string path);

        /// <summary>
        /// Attempts to build a extract a display name for this mod.
        /// </summary>
        /// <returns></returns>
        string GetDisplayName();

        /// <summary>
        /// Name / identifier for this mod. Unique within the repository.
        /// </summary>
        /// <returns></returns>
        string GetIdentifier();

        /// <summary>
        /// Returns metadata for a file. Exception if file data not found.
        /// Looks up stored value, no IO overhead.
        /// </summary>
        /// <param name="path">Relative path. Using forward slashes, starting with a forward slash, and in lower case.</param>
        /// <returns></returns>
        long GetFileSize(string path);

        /// <summary>
        /// Downloads a file. Exception if not found.
        /// </summary>
        /// <param name="path">Relative path. Using forward slashes, starting with a forward slash, and in lower case.</param>
        /// <param name="filePath">Path in local file storage, as download target.</param>
        /// <param name="updateCallback">Called occasionally with number of bytes downloaded since last call</param>
        /// <param name="token">Can be used to cancel this operation.</param>
        void DownloadTo(string path, string filePath, Action<long> updateCallback, CancellationToken token);

        /// <summary>
        /// Updates an existing file. Exception if not found.
        /// </summary>
        /// <param name="path">Relative path. Using forward slashes, starting with a forward slash, and in lower case.</param>
        /// <param name="filePath">Path in local file storage, as local target.</param>
        /// <param name="updateCallback">Called occasionally with number of bytes downloaded since last call</param>
        /// <param name="token">Can be used to cancel this operation.</param>
        void UpdateTo(string path, string filePath, Action<long> updateCallback, CancellationToken token);
        Uid GetUid();
    }
}
