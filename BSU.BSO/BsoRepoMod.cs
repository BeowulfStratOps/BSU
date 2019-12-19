﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using BSU.BSO.FileStructures;
using BSU.CoreCommon;
using BSU.Hashes;
using Newtonsoft.Json;

namespace BSU.BSO
{
    // TODO: document it's using lower case paths only!
    internal  class BsoRepoMod : IRemoteMod
    {
        private readonly string _url, _name;
        private readonly HashFile _hashFile;
        private string _displayName;

        public List<string> GetFileList() => _hashFile.Hashes.Select(h => h.FileName.ToLowerInvariant()).ToList();

        public byte[] GetFile(string path)
        {
            using var client = new WebClient();
            return client.DownloadData(_url + GetRealPath(path));
        }

        private string GetRealPath(string path) => GetFileEntry(path).FileName;

        private HashType GetFileEntry(string path) => _hashFile.Hashes.Single(h => h.FileName.ToLowerInvariant() == path);

        public BsoRepoMod(string url, string name)
        {
            _url = url;
            _name = name;

            using var client = new WebClient();
            var hashFileJson = client.DownloadString(_url + "/hash.json");
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
            client.DownloadProgressChanged += (sender, args) => updateCallback(args.BytesReceived);
            client.DownloadFile(url, filePath);
        }

        public void UpdateTo(string path, string filePath, Action<long> updateCallback)
        {
            DownloadTo(path, filePath, updateCallback);
        }
    }
}
