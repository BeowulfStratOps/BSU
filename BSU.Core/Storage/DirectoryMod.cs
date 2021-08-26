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
        private readonly Logger _logger = EntityLogger.GetLogger();

        private readonly DirectoryInfo _dir;
        private readonly IStorage _parentStorage;
        private string _displayName;

        public DirectoryMod(DirectoryInfo dir, IStorage parentStorage)
        {
            _dir = dir;
            _parentStorage = parentStorage;
        }

        public async Task Load(CancellationToken cancellationToken)
        {
            await GetDisplayName(cancellationToken);
        }

        public int GetUid() => _logger.GetId();

        /// <summary>
        /// Attempts to retrieve a display name for this mod folder.
        /// </summary>
        /// <returns></returns>
        public async Task<string> GetDisplayName(CancellationToken cancellationToken)
        {
            if (_displayName != null) return _displayName;

            var modCpp = await OpenFile("/mod.cpp", FileAccess.Read, cancellationToken);
            string modCppData = null;
            if (modCpp != null)
            {
                using var reader = new StreamReader(modCpp);
                modCppData = await reader.ReadToEndAsync();
            }

            var files = await GetFileList(cancellationToken);
            var keys = files.Where(p => Regex.IsMatch(p, "/keys/.*\\.bikey", RegexOptions.IgnoreCase))
                .Select(p => p.Split('/').Last().Replace(".bikey", "")).ToList();

            return _displayName = Util.GetDisplayName(modCppData, keys);
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
                _logger.Trace("Reading file {0}", path);
                if (access.HasFlag(FileAccess.Write))
                {
                    if (!_parentStorage.CanWrite()) throw new NotSupportedException();

                    // TODO: looks ugly
                    // TODO: async
                    Directory.CreateDirectory(new FileInfo(GetFullFilePath(path)).Directory.FullName);
                }
                // TODO: async?
                return File.Open(GetFullFilePath(path), FileMode.OpenOrCreate);
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
            var files = _dir.EnumerateFiles("*", SearchOption.AllDirectories);
            var result = files.Select(fi => fi.FullName.Replace(_dir.FullName, "").Replace('\\', '/').ToLowerInvariant())
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
            _logger.Trace("Deleting file {0}", path);
            if (!_parentStorage.CanWrite()) throw new NotSupportedException();
            // TODO: async?
            File.Delete(GetFullFilePath(path));
        }

        private string GetFullFilePath(string path)
        {
            Util.CheckPath(path);
            return Path.Combine(_dir.FullName, path.Substring(1));
        }
    }
}
