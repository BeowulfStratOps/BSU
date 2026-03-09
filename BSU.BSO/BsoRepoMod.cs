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
        private readonly string? _expectedHash;
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
                throw new FileNotFoundException(_url);
            }
            _logger.Trace($"Finsihed downloading {_url} / {path}");
            return data;
        }

        private string? GetRealPath(string path) => GetFileEntry(path)?.FileName;

        private BsoFileHash? GetFileEntry(string path) =>
            _hashFile?.Hashes.SingleOrDefault(h => h.FileName.ToLowerInvariant() == path);

        public BsoRepoMod(string url, string? expectedHash, IJobManager jobManager)
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
            _hashFile = JsonConvert.DeserializeObject<HashFile>(hashFileJson) ?? throw new InvalidDataException();
            var actualHash = _hashFile.BuildModHash();
            _logger.Debug($"Expected hash: {_expectedHash}. Actual hash: {actualHash}.");
            // expected hash is a new feature. server might not have implemented it yet.
            if (_expectedHash != null && actualHash != _expectedHash)
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
            
            // TODO: retry
            
            await _loading;

            var url = _url + GetRealPath(path);

            _logger.Trace($"Downloading content {_url} / {path}");

            var partPath = path + ".part";
            
            try
            {
                var response = await _client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
                response.EnsureSuccessStatusCode();
                await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);

                try
                {
                    await using (var fileStream = await fileSystem.OpenWrite(partPath, cancellationToken))
                    {
                        await CopyToWithProgress(stream, fileStream, 10 * 1024 * 1024, progress, cancellationToken);
                        fileStream.SetLength(fileStream.Position);
                    } 
                    // Get the hash of the file, use the extension from the original path to use the PBO logic 
                    var entry = GetFileEntry(path) ?? throw new FileNotFoundException(path);
                    var partRead = await fileSystem.OpenRead(partPath, cancellationToken) ?? throw new FileNotFoundException(partPath);
                    if ((ulong)partRead.Length != entry.FileSize)
                    {
                        await partRead.DisposeAsync();
                        throw new InvalidDataException(
                            $"Size mismatch for {path}. Expected {entry.FileSize}, got {(ulong)partRead.Length}.");
                    }
                    var partHash = await Sha1AndPboHash.BuildAsync(partRead, Utils.GetExtension(path), cancellationToken);
                    var expectedHash = new Sha1AndPboHash(entry.Hash);

                    if (!partHash.Equals(expectedHash))
                    {
                        _logger.Warn($"Hash mismatch for {path}. Expected {expectedHash}, got {partHash}");
                        await fileSystem.Delete(partPath, cancellationToken);
                        throw new InvalidDataException($"Hash mismatch for {path}");
                    }
                    
                    
                    await fileSystem.Move(partPath, path, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    _logger.Info($"Aborted downloading content {_url} / {partPath}");
                    throw;
                }

                _logger.Trace($"Finished downloading content {_url} / {partPath}");
            }
            catch (Exception e)
            {
                try
                {
                    await fileSystem.Delete(partPath, CancellationToken.None);
                }
                catch
                {
                    // best effort cleanup of temp files
                }
                _logger.Error(e, $"Error while trying to download {partPath}");
                throw;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="source"></param>
        /// <param name="destination"></param>
        /// <param name="bufferSize"></param>
        /// <param name="progress">Total Progress</param>
        /// <param name="cancellationToken"></param>
        private static async Task CopyToWithProgress(Stream source, Stream destination, int bufferSize, IProgress<ulong> progress, CancellationToken cancellationToken)
        {
            // borrowed from Stream.CopyTo
            var buffer = ArrayPool<byte>.Shared.Rent(bufferSize);
            ulong copiedTotal = 0;
            try
            {
                int read;
                while ((read = await source.ReadAsync(buffer, cancellationToken)) != 0)
                {
                    await destination.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
                    copiedTotal += (uint)read;
                    progress?.Report(copiedTotal);
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }

        public async Task UpdateTo(string path, IFileSystem fileSystem, IProgress<ulong> progress, CancellationToken cancellationToken)
        {
            // TODO: retry
            
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
                var syncProgress = new SyncProgress(progress.Report);
                Zsync.Sync(controlFile, new List<Stream> { seed }, downloader, fileStream, syncProgress, cancellationToken);
                fileStream.SetLength(fileStream.Position);
                await seed.DisposeAsync();
                await fileStream.DisposeAsync();

                var extension = Utils.GetExtension(path).ToLowerInvariant();
                if (extension == "pbo" || extension == "ebo")
                {
                    await VerifyWithControlFile(path, partPath, controlFile, fileSystem, cancellationToken);
                }

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

        private static async Task VerifyWithControlFile(string path, string partPath, ControlFile controlFile,
            IFileSystem fileSystem, CancellationToken cancellationToken)
        {
            var header = controlFile.GetHeader();
            var localPart = await fileSystem.OpenRead(partPath, cancellationToken);
            if (localPart == null) throw new FileNotFoundException(partPath);

            await using (localPart)
            {
                if (localPart.Length != header.Length)
                    throw new InvalidDataException(
                        $"Zsync length mismatch for {path}: expected {header.Length}, got {localPart.Length}.");
            }

            // Verify pass: if any range download is required, the part file does not match the control file.
            var verifyRead = await fileSystem.OpenRead(partPath, cancellationToken);
            if (verifyRead == null) throw new FileNotFoundException(partPath);
            await using (verifyRead)
            {
                Zsync.Sync(controlFile, new List<Stream> { verifyRead }, new NoDownloadRangeDownloader(path), Stream.Null, null,
                    cancellationToken);
            }
        }

        private sealed class NoDownloadRangeDownloader : IRangeDownloader
        {
            private readonly string _path;

            public NoDownloadRangeDownloader(string path)
            {
                _path = path;
            }

            public Stream DownloadRange(long from, long to) =>
                throw new InvalidDataException(
                    $"Zsync verify failed for {_path}: unexpected range request [{from}, {to}).");

            public Stream Download() =>
                throw new InvalidDataException($"Zsync verify failed for {_path}: unexpected full download request.");
        }

        private class SyncProgress : IProgress<ulong>
        {
            private readonly Action<ulong> _reportTotal;
            private ulong _total = 0;

            public SyncProgress(Action<ulong> reportTotal)
            {
                _reportTotal = reportTotal;
            }
            
            public void Report(ulong value)
            {
                _total += value;
                _reportTotal(_total);
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
