using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using BSU.BSO.FileStructures;
using BSU.CoreInterface;
using BSU.Hashes;
using Newtonsoft.Json;

namespace BSU.BSO
{
    class BsoRepoMod : IRemoteMod
    {
        private readonly string _url, _name;
        private readonly HashFile _hashFile;

        public List<string> GetFileList() => _hashFile.Hashes.Select(h => h.FileName).ToList();

        public byte[] GetFile(string path)
        {
            using var client = new WebClient();
            return client.DownloadData(_url + path);
        }

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
            string modCpp = null;
            if (_hashFile.Hashes.Any(h => h.FileName == "/mod.cpp"))
            {
                using var client = new WebClient();
                modCpp = client.DownloadString(_url + "/mod.cpp");
            }

            var keys = _hashFile.Hashes.Where(h => h.FileName.EndsWith(".bikey"))
                .Select(h => h.FileName.Split('/')[^1].Replace(".bikey", "")).ToList();

            keys = keys.Any() ? keys : null;

            return Util.Util.GetDisplayName(modCpp, keys);
        }

        public string GetIdentifier() => _name;

        public string GetVersionIdentifier()
        {
            throw new NotImplementedException();
        }

        public FileHash GetFileHash(string path)
        {
            var entry = _hashFile.Hashes.SingleOrDefault(h => h.FileName == path);
            return entry == null ? null : new SHA1AndPboHash(entry.Hash, entry.FileSize);
        }

        public long GetFileSize(string path) => _hashFile.Hashes.Single(h => h.FileName == path).FileSize;

        public void DownloadTo(string path, string filePath, Action<long> updateCallback)
        {
            var url = _url + path; // check for existence first?

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
