using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BSU.Core.Concurrency;
using BSU.CoreCommon;
using BSU.CoreCommon.Hashes;
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
            return await Sha1AndPboHash.BuildAsync(file, extension, cancellationToken);
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

        public Task<Stream> OpenWrite(string path, CancellationToken cancellationToken)
        {
            if (!_parentStorage.CanWrite())
                throw new InvalidOperationException();

            Logger.Trace($"Writing file {path}");

            var filePath = GetFullFilePath(path);

            // TODO: looks ugly
            // TODO: async?
            Directory.CreateDirectory(new FileInfo(filePath).Directory!.FullName);
            // TODO: async?
            return Task.FromResult<Stream>(File.Open(filePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None));
        }

        public Task<Stream?> OpenRead(string path, CancellationToken cancellationToken)
        {
            Logger.Trace($"Reading file {path}");
            try
            {
                // TODO: async?
                return Task.FromResult<Stream?>(File.Open(GetFullFilePath(path), FileMode.Open, FileAccess.Read, FileShare.Read));
            }
            catch (FileNotFoundException)
            {
                return Task.FromResult<Stream?>(null);
            }
            catch (DirectoryNotFoundException)
            {
                return Task.FromResult<Stream?>(null);
            }
        }

        public Task Move(string from, string to, CancellationToken cancellationToken)
        {
            if (!_parentStorage.CanWrite()) throw new NotSupportedException();
            Logger.Trace($"Moving file {from} to {to}");
            // TODO: async?
            File.Move(GetFullFilePath(from), GetFullFilePath(to), true);
            return Task.CompletedTask;
        }

        public Task<bool> HasFile(string path, CancellationToken cancellationToken)
        {
            // TODO: async?
            return Task.FromResult(File.Exists(GetFullFilePath(path)));
        }

        public Task Delete(string path, CancellationToken cancellationToken)
        {
            if (!_parentStorage.CanWrite()) throw new NotSupportedException();
            Logger.Trace($"Deleting file {path}");
            // TODO: async?
            File.Delete(GetFullFilePath(path));
            return Task.CompletedTask;
        }

        public Dictionary<Type, Func<CancellationToken, Task<IModHash>>> GetHashFunctions() => new()
        {
            { typeof(VersionHash), WrapHashFunc(ct => VersionHash.CreateAsync(this, ct)) },
            { typeof(MatchHash), WrapHashFunc(ct => MatchHash.CreateAsync(this, ct)) }
        };

        private static Func<CancellationToken, Task<IModHash>> WrapHashFunc<T>(Func<CancellationToken, Task<T>> func)
            where T : IModHash => async ct => await Task.Run(() => func(ct), ct);
    }
}
