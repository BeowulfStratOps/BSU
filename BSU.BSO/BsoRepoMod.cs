using System;
using System.Collections.Generic;
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
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private readonly string _url;
        private HashFile _hashFile;
        private string _displayName;

        private readonly Uid _uid = new Uid();

        public List<string> GetFileList() => _hashFile.Hashes.Select(h => h.FileName.ToLowerInvariant()).ToList();

        /// <summary>
        /// Downloads a single file. Exception if file is not found.
        /// </summary>
        /// <param name="path">Relative path. Using forward slashes, starting with a forward slash, and in lower case.</param>
        /// <returns></returns>
        public byte[] GetFile(string path)
        {
            using var client = new WebClient();
            Logger.Debug("{0} Downloading file from {1} / {2}", _uid, _url, path);
            var data = client.DownloadData(_url + GetRealPath(path));
            Logger.Debug("{0} Finsihed downloading {1} / {2}", _uid, _url, path);
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
            Logger.Debug("{0} Downloading hash file from {1}", _uid, _url);
            var hashFileJson = client.DownloadString(_url + "/hash.json");
            Logger.Debug("{0} Finished downloading hash file", _uid);
            _hashFile = JsonConvert.DeserializeObject<HashFile>(hashFileJson);
            GetDisplayName();
#if SlowMode
            Thread.Sleep(1337);
#endif
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
                Logger.Debug("{0} Downloading mod.cpp from {1}", _uid, _url);
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
        /// <param name="filePath">Path in local file storage, as download target.</param>
        /// <param name="updateCallback">Called occasionally with number of bytes downloaded since last call</param>
        /// <param name="token">Can be used to cancel this operation.</param>
        public void DownloadTo(string path, string filePath, Action<long> updateCallback, CancellationToken token)
        {
            var url = _url + GetRealPath(path);

            using var client = new WebClient();
            Logger.Debug("{0} Downloading content {1} / {2}", _uid, _url, path);
            client.DownloadProgressChanged += (sender, args) => updateCallback(args.BytesReceived);
            client.DownloadFile(url, filePath);
            token.Register(client.CancelAsync);
            Logger.Debug("{0} Finished downloading content {1} / {2}", _uid, _url, path);
        }

        /// <summary>
        /// Updates an existing file. Exception if not found.
        /// </summary>
        /// <param name="path">Relative path. Using forward slashes, starting with a forward slash, and in lower case.</param>
        /// <param name="filePath">Path in local file storage, as local target.</param>
        /// <param name="updateCallback">Called occasionally with number of bytes downloaded since last call</param>
        /// <param name="token">Can be used to cancel this operation.</param>
        public void UpdateTo(string path, string filePath, Action<long> updateCallback, CancellationToken token)
        {
            DownloadTo(path, filePath, updateCallback, token);
        }

        public Uid GetUid() => _uid;
    }
}
