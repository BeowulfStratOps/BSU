using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
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
        private readonly Logger _logger = EntityLogger.GetLogger();

        private readonly string _url;
        private HashFile _hashFile;
        private string _displayName;

        public List<string> GetFileList() => _hashFile.Hashes.Select(h => h.FileName.ToLowerInvariant()).ToList();

        /// <summary>
        /// Downloads a single file. Exception if file is not found.
        /// </summary>
        /// <param name="path">Relative path. Using forward slashes, starting with a forward slash, and in lower case.</param>
        /// <returns></returns>
        public byte[] GetFile(string path)
        {
            using var client = new WebClient();
            _logger.Debug("Downloading file from {0} / {1}", _url, path);
            var data = client.DownloadData(_url + GetRealPath(path));
            _logger.Debug("Finsihed downloading {0} / {1}", _url, path);
            return data;
        }

        private string GetRealPath(string path) => GetFileEntry(path)?.FileName;

        private HashType GetFileEntry(string path) =>
            _hashFile.Hashes.SingleOrDefault(h => h.FileName.ToLowerInvariant() == path);

        public BsoRepoMod(string url)
        {
            _url = url;
        }

        public void Load()
        {
            using var client = new WebClient();
            _logger.Debug("Downloading hash file from {0}", _url);
            var hashFileJson = client.DownloadString(_url + "/hash.json");
            _logger.Debug("Finished downloading hash file");
            _hashFile = JsonConvert.DeserializeObject<HashFile>(hashFileJson);
            GetDisplayName();
        }


        /// <summary>
        /// Attempts to build a extract a display name for this mod. Cached.
        /// </summary>
        /// <returns></returns>
        public string GetDisplayName()
        {
            if (_displayName != null) return _displayName;

            string modCpp = null;
            var modCppEntry = GetFileEntry("/mod.cpp");
            if (modCppEntry != null)
            {
                using var client = new WebClient();
                _logger.Debug("Downloading mod.cpp from {0}", _url);
                modCpp = client.DownloadString(_url + modCppEntry.FileName);
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
        public FileHash GetFileHash(string path)
        {
            var entry = GetFileEntry(path);
            return entry == null ? null : new SHA1AndPboHash(entry.Hash, entry.FileSize);
        }

        /// <summary>
        /// Returns metadata for a file. Exception if file data not found.
        /// Looks up stored value, no IO overhead.
        /// </summary>
        /// <param name="path">Relative path. Using forward slashes, starting with a forward slash, and in lower case.</param>
        /// <returns></returns>
        public long GetFileSize(string path) => GetFileEntry(path).FileSize;

        /// <summary>
        /// Downloads a file. Exception if not found.
        /// </summary>
        /// <param name="path">Relative path. Using forward slashes, starting with a forward slash, and in lower case.</param>
        /// <param name="updateCallback">Called occasionally with number of bytes downloaded since last call</param>
        /// <param name="token">Can be used to cancel this operation.</param>
        public void DownloadTo(string path, Stream fileStream, Action<long> updateCallback, CancellationToken token)
        {
            // TODO: use .part file
            // TODO: use FileStream
            var url = _url + GetRealPath(path);

            _logger.Debug("Downloading content {0} / {1}", _url, path);

            var req = WebRequest.CreateHttp(url);
            using var resp = req.GetResponse();
            using var stream = resp.GetResponseStream();

            var buffer = new byte[10 * 1024 * 1024];
            while (!token.IsCancellationRequested)
            {
                var len = stream.Read(buffer, 0, buffer.Length);
                if (len == 0) break;
                fileStream.Write(buffer, 0, len);
                updateCallback(len);
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
        /// <param name="updateCallback">Called occasionally with number of bytes downloaded since last call</param>
        /// <param name="token">Can be used to cancel this operation.</param>
        public void UpdateTo(string path, Stream fileStream, Action<long> updateCallback, CancellationToken token)
        {
            DownloadTo(path, fileStream, updateCallback, token);
        }

        public int GetUid() => _logger.GetId();
    }
}
