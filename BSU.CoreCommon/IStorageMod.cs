using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using BSU.Hashes;

namespace BSU.CoreCommon
{
    public interface IStorageMod : IFileSystem
    {
        Task<List<string>> GetFileList(CancellationToken cancellationToken);

        /// <summary>
        /// Get hash of a local file. Null if it doesn't exist.
        /// </summary>
        /// <param name="path">Relative path. Using forward slashes, starting with a forward slash, and in lower case.</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<FileHash> GetFileHash(string path, CancellationToken cancellationToken);

        Task<string> GetTitle(CancellationToken cancellationToken);
        string Path { get; }
    }
}
