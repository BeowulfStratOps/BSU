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
            return Task.FromResult(result);
        }

        /// <summary>
        /// Get hash of a local file. Exception if it doesn't exist.
        /// </summary>
        /// <param name="path">Relative path. Using forward slashes, starting with a forward slash, and in lower case.</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<FileHash> GetFileHash(string path, CancellationToken cancellationToken)
        {
            ModUtil.CheckPath(path);
            var extension = Utils.GetExtension(path).ToLowerInvariant();
            var file = await OpenRead(path, cancellationToken);
            if (file == null) throw new FileNotFoundException(path);
            return await SHA1AndPboHash.BuildAsync(file, extension, cancellationToken);
        }

        public virtual Task<string> GetTitle(CancellationToken cancellationToken)
        {
            return Task.FromResult(Dir.Name);
        }

        public string Path => Dir.FullName;

        private string GetFullFilePath(string path)
        {
            // TODO: check that the path is in the mod directory (avoid accidental directory traversal)
            ModUtil.CheckPath(path);
            return System.IO.Path.Combine(Dir.FullName, path.Substring(1));
        }

        public async Task<Stream> OpenWrite(string path, CancellationToken cancellationToken)
        {
            if (!_parentStorage.CanWrite())
                throw new InvalidOperationException();

            Logger.Trace($"Writing file {path}");

            var filePath = GetFullFilePath(path);

            // TODO: looks ugly
            // TODO: async?
            Directory.CreateDirectory(new FileInfo(filePath).Directory!.FullName);
            // TODO: async?
            return File.Open(filePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        }

        public async Task<Stream?> OpenRead(string path, CancellationToken cancellationToken)
        {
            Logger.Trace($"Reading file {path}");
            try
            {
                // TODO: async?
                return File.Open(GetFullFilePath(path), FileMode.Open, FileAccess.Read, FileShare.Read);
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

        public async Task Move(string from, string to, CancellationToken cancellationToken)
        {
            if (!_parentStorage.CanWrite()) throw new NotSupportedException();
            Logger.Trace($"Moving file {from} to {to}");
            // TODO: async?
            File.Move(GetFullFilePath(from), GetFullFilePath(to), true);
        }

        public async Task<bool> HasFile(string path, CancellationToken cancellationToken)
        {
            // TODO: async?
            return File.Exists(GetFullFilePath(path));
        }

        public async Task Delete(string path, CancellationToken cancellationToken)
        {
            if (!_parentStorage.CanWrite()) throw new NotSupportedException();
            Logger.Trace($"Deleting file {path}");
            // TODO: async?
            File.Delete(GetFullFilePath(path));
        }
    }
}
