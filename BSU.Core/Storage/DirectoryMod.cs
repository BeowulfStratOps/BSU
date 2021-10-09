using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using BSU.CoreCommon;
using BSU.Hashes;
using NLog;

namespace BSU.Core.Storage
{
    /// <summary>
    /// Local mod folder.
    /// </summary>
    public class DirectoryMod : IStorageMod
    {
        protected readonly ILogger Logger;

        protected readonly DirectoryInfo Dir;
        private readonly IStorage _parentStorage;

        public DirectoryMod(DirectoryInfo dir, IStorage parentStorage)
        {
            Logger = LogHelper.GetLoggerWithIdentifier(this, dir.Name);
            Dir = dir;
            _parentStorage = parentStorage;
        }

        /// <summary>
        /// Returns a read-only file stream. Must be disposed.
        /// </summary>
        /// <param name="path">Relative path. Using forward slashes, starting with a forward slash, and in lower case.</param>
        /// <returns></returns>
        public async Task<Stream> OpenFile(string path, FileAccess access, CancellationToken cancellationToken)
        {
            try
            {
                Logger.Trace("Reading file {0}", path);
                var mode = FileMode.Open;
                var share = FileShare.Read;
                if (access.HasFlag(FileAccess.Write))
                {
                    if (!_parentStorage.CanWrite()) throw new NotSupportedException();

                    mode = FileMode.OpenOrCreate;
                    share = FileShare.None;

                    // TODO: looks ugly
                    // TODO: async
                    Directory.CreateDirectory(new FileInfo(GetFullFilePath(path)).Directory.FullName);
                }
                // TODO: async?
                return File.Open(GetFullFilePath(path), mode, access, share);
            }
            catch (FileNotFoundException)
            {
                return null;
            }
            catch (DirectoryNotFoundException)
            {
                return null;
            }
        }

        /// <summary>
        /// Returns a list of relative file paths.
        /// Relative path. Using forward slashes, starting with a forward slash, and in lower case.
        /// </summary>
        /// <returns></returns>
        public Task<List<string>> GetFileList(CancellationToken cancellationToken)
        {
            // TODO: make async
            var files = Dir.EnumerateFiles("*", SearchOption.AllDirectories);
            var result = files.Select(fi => fi.FullName.Replace(Dir.FullName, "").Replace('\\', '/').ToLowerInvariant())
                .ToList();
            return Task.FromResult<List<string>>(result);
        }

        /// <summary>
        /// Get hash of a local file. Null if it doesn't exist.
        /// </summary>
        /// <param name="path">Relative path. Using forward slashes, starting with a forward slash, and in lower case.</param>
        /// <returns></returns>
        public async Task<FileHash> GetFileHash(string path, CancellationToken cancellationToken)
        {
            Util.CheckPath(path);
            var extension = Utils.GetExtension(path).ToLowerInvariant();
            var file = await OpenFile(path, FileAccess.Read, cancellationToken);
            if (file == null) return null;
            return await SHA1AndPboHash.BuildAsync(file, extension, cancellationToken);
        }

        public IStorage GetStorage() => _parentStorage;

        /// <summary>
        /// Deletes a file. Exception if it doesn't exists.
        /// </summary>
        /// <param name="path">Relative path. Using forward slashes, starting with a forward slash, and in lower case.</param>
        /// <exception cref="NotSupportedException">Not supported for read-only locations.</exception>
        public async Task DeleteFile(string path, CancellationToken cancellationToken)
        {
            Logger.Trace("Deleting file {0}", path);
            if (!_parentStorage.CanWrite()) throw new NotSupportedException();
            // TODO: async?
            File.Delete(GetFullFilePath(path));
        }

        public virtual Task<string> GetTitle(CancellationToken cancellationToken)
        {
            return Task.FromResult(Dir.Name);
        }

        private string GetFullFilePath(string path)
        {
            Util.CheckPath(path);
            return Path.Combine(Dir.FullName, path.Substring(1));
        }
    }
}
