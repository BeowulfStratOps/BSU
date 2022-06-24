using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using BSU.BSO.FileStructures;
using BSU.CoreCommon;
using BSU.CoreCommon.Hashes;
using BSU.Hashes;
using Newtonsoft.Json;
using NLog;
using zsyncnet;
using zsyncnet.Sync;

namespace BSU.BSO
{
    /// <summary>
    /// A single repository mod in BSO format.
    /// </summary>
    internal class BsoRepoMod : IRepositoryMod
    {
        // TODO: re-use http clients?

        private readonly Logger _logger = LogManager.GetCurrentClassLogger();

        private readonly string _url;
        private readonly string _expectedHash;
        private HashFile? _hashFile;
        private readonly Task _loading;
        private readonly HttpClient _client = new();


        public async Task<List<string>> GetFileList(CancellationToken cancellationToken)
        {
            await _loading;
            var files = _hashFile!.Hashes.Select(h => h.FileName.ToLowerInvariant()).ToList();
            return files;
        }

        /// <summary>
        /// Downloads a single file. Exception if file is not found.
        /// </summary>
        /// <param name="path">Relative path. Using forward slashes, starting with a forward slash, and in lower case.</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<byte[]> GetFile(string path, CancellationToken cancellationToken)
        {
            await _loading;
            _logger.Trace($"Downloading file from {_url} / {path}");
            byte[] data;
            try
            {
                data = await _client.GetByteArrayAsync(_url + GetRealPath(path), cancellationToken);
            }
            catch (Exception e)
            {
                _logger.Error(e, $"Failed to download {_url} / {path}");
                throw;
            }
            _logger.Trace($"Finsihed downloading {_url} / {path}");
            return data;
        }

        private string? GetRealPath(string path) => GetFileEntry(path)?.FileName;

        private BsoFileHash? GetFileEntry(string path) =>
            _hashFile?.Hashes.SingleOrDefault(h => h.FileName.ToLowerInvariant() == path);

        public BsoRepoMod(string url, string expectedHash, IJobManager jobManager)
        {
            _url = url;
            _expectedHash = expectedHash;
            _loading = jobManager.Run($"Bso Repo Mod Load {_url}", () => Load(CancellationToken.None), CancellationToken.None);
        }

        private async Task Load(CancellationToken cancellationToken)
        {
            using var client = new HttpClient();
            _logger.Trace($"Downloading hash file from {_url}");
            var hashFileJson = await client.GetStringAsync(_url + "/hash.json", cancellationToken);
            _logger.Trace("Finished downloading hash file");
            _hashFile = JsonConvert.DeserializeObject<HashFile>(hashFileJson);
            var actualHash = _hashFile.BuildModHash();
            _logger.Debug($"Expected hash: {_expectedHash}. Actual hash: {actualHash}.");
            if (actualHash != _expectedHash)
                // TODO: will have the mod stuck on loading
                throw new InvalidDataException($"Expected hash: {_expectedHash}. Actual hash: {actualHash}.");
        }


        /// <summary>
        /// Attempts to build a extract a display name for this mod. Cached.
        /// </summary>
        /// <returns></returns>
        public async Task<(string name, string version)> GetDisplayInfo(CancellationToken cancellationToken)
        {
            await _loading;

            string? modCpp = null;
            var modCppEntry = GetFileEntry("/mod.cpp");
            if (modCppEntry != null)
            {
                using var client = new HttpClient();
                _logger.Trace($"Downloading mod.cpp from {_url}");
                modCpp = await client.GetStringAsync(_url + modCppEntry.FileName, cancellationToken);
            }

            // TODO: make case insensitive
            var keys = _hashFile!.Hashes.Where(h => h.FileName.EndsWith(".bikey"))
                .Select(h => h.FileName.Split('/')[^1].Replace(".bikey", "")).ToList();

            keys = keys.Any() ? keys : null;

            return ModUtil.GetDisplayInfo(modCpp, keys);
        }

        /// <summary>
        /// Returns metadata for a file. Null if file data not found.
        /// Looks up stored value, no IO overhead.
        /// </summary>
        /// <param name="path">Relative path. Using forward slashes, starting with a forward slash, and in lower case.</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<FileHash> GetFileHash(string path, CancellationToken cancellationToken)
        {
            await _loading;
            var entry = GetFileEntry(path);
            if (entry == null) throw new InvalidOperationException();
            return new Sha1AndPboHash(entry.Hash);
        }

        /// <summary>
        /// Returns metadata for a file. Exception if file data not found.
        /// Looks up stored value, no IO overhead.
        /// </summary>
        /// <param name="path">Relative path. Using forward slashes, starting with a forward slash, and in lower case.</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<ulong> GetFileSize(string path, CancellationToken cancellationToken)
        {
            await _loading;
            return GetFileEntry(path)?.FileSize ?? throw new FileNotFoundException(path);
        }

        public async Task DownloadTo(string path, IFileSystem fileSystem, IProgress<ulong> progress, CancellationToken cancellationToken)
        {
            await _loading;

            var url = _url + GetRealPath(path);

            _logger.Trace($"Downloading content {_url} / {path}");

            await using var fileStream = await fileSystem.OpenWrite(path, cancellationToken);

            try
            {
                var response = await _client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
                response.EnsureSuccessStatusCode();
                await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);

                try
                {
                    await CopyToWithProgress(stream, fileStream, 10 * 1024 * 1024, progress, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    _logger.Info($"Aborted downloading content {_url} / {path}");
                    throw;
                }

                fileStream.SetLength(fileStream.Position);
                _logger.Trace($"Finished downloading content {_url} / {path}");
            }
            catch (Exception e)
            {
                _logger.Error(e, $"Error while trying to download {path}");
                throw;
            }
        }

        private static async Task CopyToWithProgress(Stream source, Stream destination, int bufferSize, IProgress<ulong> progress, CancellationToken cancellationToken)
        {
            // borrowed from Stream.CopyTo
            var buffer = ArrayPool<byte>.Shared.Rent(bufferSize);
            try
            {
                int read;
                while ((read = await source.ReadAsync(buffer, cancellationToken)) != 0)
                {
                    await destination.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
                    progress?.Report((ulong)read);
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }

        public async Task UpdateTo(string path, IFileSystem fileSystem, IProgress<ulong> progress, CancellationToken cancellationToken)
        {
            await _loading;

            _logger.Debug($"Updating file {_url} / {path}");

            var url = _url + GetRealPath(path);
            await using var cfData = await _client.GetStreamAsync(url + ".zsync", cancellationToken);
            var controlFile = new ControlFile(cfData);

            var downloader = new RangeDownloader(new Uri(url), _client);

            var partPath = path + ".part";
            var fileStream = await fileSystem.OpenWrite(partPath, cancellationToken);
            var seed = await fileSystem.OpenRead(path, cancellationToken);

            try
            {

                if (seed == null) throw new InvalidOperationException();
                Zsync.Sync(controlFile, new List<Stream> { seed }, downloader, fileStream, progress, cancellationToken);
                await seed.DisposeAsync();
                await fileStream.DisposeAsync();
                await fileSystem.Move(partPath, path, cancellationToken);
                _logger.Debug($"Finished updating file {_url} / {path}");
            }
            catch (Exception e)
            {
                if (seed != null)
                    await seed.DisposeAsync();
                await fileStream.DisposeAsync();
                _logger.Error(e, $"Error while syncing {_url} / {path}");
                throw;
            }
        }

        public async Task<HashCollection> GetHashes(CancellationToken cancellationToken)
        {
            var versionHash = await VersionHash.CreateAsync(this, cancellationToken);
            var matchHash = await MatchHash.CreateAsync(this, cancellationToken);
            return new HashCollection(versionHash, matchHash);
        }
    }
}
