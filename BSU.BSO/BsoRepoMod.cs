using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using BSU.Util;
using BSU.BSO.FileStructures;
using BSU.BSO.Hashes;
using BSU.CoreInterface;
using Newtonsoft.Json;

namespace BSU.BSO
{
    class BsoRepoMod : IRemoteMod
    {
        private readonly string _url, _name;
        private readonly HashFile _hashFile;

        public List<HashType> GetFileList() => _hashFile.Hashes;

        public byte[] DownloadFile(string path)
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

        public List<ILocalMod> GetMatchingMods(List<ILocalMod> allLocalMods)
        {
            var remote = MatchHash.FromRemoteMod(this);
            var result = new List<ILocalMod>();

            foreach (var localMod in allLocalMods)
            {
                var local = MatchHash.FromLocalMod(localMod);
                if (local.IsMatch(remote)) result.Add(localMod);
            }

            return result;
        }

        public bool IsVersionMatching(ILocalMod localMod)
        {
            throw new NotImplementedException();
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

        public ISyncState PrepareSync(ILocalMod target)
        {
            throw new NotImplementedException();
        }

        public string GetIdentifier() => _name;

        public string GetVersionIdentifier()
        {
            throw new NotImplementedException();
        }
    }
}
