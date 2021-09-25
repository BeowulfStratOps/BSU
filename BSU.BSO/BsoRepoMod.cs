using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using BSU.BSO.FileStructures;
using BSU.CoreCommon;
using BSU.Hashes;
using Newtonsoft.Json;
using NLog;

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
        private HashFile _hashFile;
        private string _displayName;
        private readonly Task _loading;


        public async Task<List<string>> GetFileList(CancellationToken cancellationToken)
        {
            await _loading;
            var files = _hashFile.Hashes.Select(h => h.FileName.ToLowerInvariant()).ToList();
            return files;
        }

        /// <summary>
        /// Downloads a single file. Exception if file is not found.
        /// </summary>
        /// <param name="path">Relative path. Using forward slashes, starting with a forward slash, and in lower case.</param>
        /// <returns></returns>
        public async Task<byte[]> GetFile(string path, CancellationToken cancellationToken)
        {
            await _loading;
            using var client = new HttpClient();
            _logger.Debug("Downloading file from {0} / {1}", _url, path);
            var data = await client.GetByteArrayAsync(_url + GetRealPath(path), cancellationToken);
            _logger.Debug("Finsihed downloading {0} / {1}", _url, path);
            return data;
        }

        private string GetRealPath(string path) => GetFileEntry(path)?.FileName;

        private HashType GetFileEntry(string path) =>
            _hashFile.Hashes.SingleOrDefault(h => h.FileName.ToLowerInvariant() == path);

        public BsoRepoMod(string url)
        {
            _url = url;
            _loading = Task.Run(() => Load(CancellationToken.None));
        }

        private async Task Load(CancellationToken cancellationToken)
        {
            using var client = new HttpClient();
            _logger.Debug("Downloading hash file from {0}", _url);
            var hashFileJson = await client.GetStringAsync(_url + "/hash.json", cancellationToken);
            _logger.Debug("Finished downloading hash file");
            _hashFile = JsonConvert.DeserializeObject<HashFile>(hashFileJson);
        }


        /// <summary>
        /// Attempts to build a extract a display name for this mod. Cached.
        /// </summary>
        /// <returns></returns>
        public async Task<string> GetDisplayName(CancellationToken cancellationToken)
        {
            if (_displayName != null) return _displayName;
            await _loading;

            string modCpp = null;
            var modCppEntry = GetFileEntry("/mod.cpp");
            if (modCppEntry != null)
            {
                using var client = new HttpClient();
                _logger.Debug("Downloading mod.cpp from {0}", _url);
                modCpp = await client.GetStringAsync(_url + modCppEntry.FileName, cancellationToken);
            }

            // TODO: make case insensitive
            var keys = _hashFile.Hashes.Where(h => h.FileName.EndsWith(".bikey"))
                .Select(h => h.FileName.Split('/')[^1].Replace(".bikey", "")).ToList();

            keys = keys.Any() ? keys : null;

            return _displayName = Util.GetDisplayName(modCpp, keys);
        }

        /// <summary>
        /// Returns metadata for a file. Null if file data not found.
        /// Looks up stored value, no IO overhead.
        /// </summary>
        /// <param name="path">Relative path. Using forward slashes, starting with a forward slash, and in lower case.</param>
        /// <returns></returns>
        public async Task<FileHash> GetFileHash(string path, CancellationToken cancellationToken)
        {
            await _loading;
            var entry = GetFileEntry(path);
            var hash = entry == null ? null : new SHA1AndPboHash(entry.Hash);
            return hash;
        }

        /// <summary>
        /// Returns metadata for a file. Exception if file data not found.
        /// Looks up stored value, no IO overhead.
        /// </summary>
        /// <param name="path">Relative path. Using forward slashes, starting with a forward slash, and in lower case.</param>
        /// <returns></returns>
        public async Task<long> GetFileSize(string path, CancellationToken cancellationToken)
        {
            await _loading;
            return GetFileEntry(path).FileSize;
        }

        /// <summary>
        /// Downloads a file. Exception if not found.
        /// </summary>
        /// <param name="path">Relative path. Using forward slashes, starting with a forward slash, and in lower case.</param>
        /// <param name="progress">Called occasionally with number of bytes downloaded since last call</param>
        /// <param name="token">Can be used to cancel this operation.</param>
        public async Task DownloadTo(string path, Stream fileStream, IProgress<long> progress, CancellationToken token)
        {
            await _loading;
            // TODO: use .part file
            // TODO: use FileStream
            var url = _url + GetRealPath(path);

            _logger.Debug("Downloading content {0} / {1}", _url, path);

            var req = WebRequest.CreateHttp(url);
            using var resp = await req.GetResponseAsync();
            await using var stream = resp.GetResponseStream();

            var buffer = new byte[10 * 1024 * 1024];
            while (!token.IsCancellationRequested)
            {
                var len = await stream.ReadAsync(buffer, 0, buffer.Length, token);
                if (len == 0) break;
                await fileStream.WriteAsync(buffer, 0, len, token);
                progress.Report(len);
            }

            if (token.IsCancellationRequested)
            {
                _logger.Debug("Aborted downloading content {0} / {1}", _url, path);
                throw new OperationCanceledException();
            }

            fileStream.SetLength(fileStream.Position);
            _logger.Debug("Finished downloading content {0} / {1}", _url, path);
        }

        /// <summary>
        /// Updates an existing file. Exception if not found.
        /// </summary>
        /// <param name="path">Relative path. Using forward slashes, starting with a forward slash, and in lower case.</param>
        /// <param name="progress">Called occasionally with number of bytes downloaded since last call</param>
        /// <param name="token">Can be used to cancel this operation.</param>
        public async Task UpdateTo(string path, Stream fileStream, IProgress<long> progress, CancellationToken token)
        {
            await _loading;
            await DownloadTo(path, fileStream, progress, token);
        }
    }
}
