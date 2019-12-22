using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using BSU.BSO.FileStructures;
using BSU.CoreCommon;
using BSU.Hashes;
using Newtonsoft.Json;
using NLog;
using NLog.Fluent;

namespace BSU.BSO
{
    // TODO: document it's using lower case paths only!
    internal  class BsoRepoMod : IRepositoryMod
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private readonly string _url, _name;
        private readonly HashFile _hashFile;
        private string _displayName;

        private readonly Uid _uid = new Uid();

        public List<string> GetFileList() => _hashFile.Hashes.Select(h => h.FileName.ToLowerInvariant()).ToList();

        public byte[] GetFile(string path)
        {
            using var client = new WebClient();
            Logger.Debug("{0} Downloading content file from {1} / {2}", _uid, _url, path);
            var data = client.DownloadData(_url + GetRealPath(path));
            Logger.Debug("{0} Finsihed downloading content file from {1} / {2}", _uid, _url, path);
            return data;
        }

        private string GetRealPath(string path) => GetFileEntry(path).FileName;

        private HashType GetFileEntry(string path) => _hashFile.Hashes.Single(h => h.FileName.ToLowerInvariant() == path);

        public BsoRepoMod(string url, string name)
        {
            _url = url;
            _name = name;

            using var client = new WebClient();
            Logger.Debug("{0} Downloading hash file from {1}", _uid, _url);
            var hashFileJson = client.DownloadString(_url + "/hash.json");
            Logger.Debug("{0} Finished downloading hash file from {1}", _uid, _url);
            _hashFile = JsonConvert.DeserializeObject<HashFile>(hashFileJson);
        }

        public string GetDisplayName()
        {
            if (_displayName != null) return _displayName;

            string modCpp = null;
            var path = GetRealPath("/mod.cpp");
            if (_hashFile.Hashes.Any(h => h.FileName == path))
            {
                using var client = new WebClient();
                Logger.Debug("{0} Downloading mod.cpp from {1}", _uid, _url);
                modCpp = client.DownloadString(_url + path);
            }

            // TODO: make case insensitive
            var keys = _hashFile.Hashes.Where(h => h.FileName.EndsWith(".bikey"))
                .Select(h => h.FileName.Split('/')[^1].Replace(".bikey", "")).ToList();

            keys = keys.Any() ? keys : null;

            return _displayName = Util.GetDisplayName(modCpp, keys);
        }

        public string GetIdentifier() => _name;

        public FileHash GetFileHash(string path)
        {
            var entry = GetFileEntry(path);
            return entry == null ? null : new SHA1AndPboHash(entry.Hash, entry.FileSize);
        }

        public long GetFileSize(string path) => GetFileEntry(path).FileSize;

        public void DownloadTo(string path, string filePath, Action<long> updateCallback)
        {
            var url = _url + GetRealPath(path);

            using var client = new WebClient();
            Logger.Debug("{0} Downloading content {1} / {2}", _uid, _url, path);
            client.DownloadProgressChanged += (sender, args) => updateCallback(args.BytesReceived);
            client.DownloadFile(url, filePath);
            Logger.Debug("{0} Finished downloading content {1} / {2}", _uid, _url, path);
        }

        public void UpdateTo(string path, string filePath, Action<long> updateCallback)
        {
            DownloadTo(path, filePath, updateCallback);
        }

        public Uid GetUid() => _uid;
    }
}
