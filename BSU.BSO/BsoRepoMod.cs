using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using BSU.Util;
using BSU.BSO.FileStructures;
using BSU.CoreInterface;
using Newtonsoft.Json;

namespace BSU.BSO
{
    class BsoRepoMod : IRemoteMod
    {
        private readonly string _url, _name;
        private readonly HashFile _hashFile;

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
            throw new NotImplementedException();
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

            return ModDisplayName.GetDisplayName(modCpp, keys);
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
