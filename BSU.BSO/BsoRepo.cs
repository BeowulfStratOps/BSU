using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using BSU.BSO.FileStructures;
using BSU.CoreCommon;
using Newtonsoft.Json;

namespace BSU.BSO
{
    public class BsoRepo : IRepository
    {
        private readonly string _url, _name;
        private readonly List<IRemoteMod> _mods;

        public BsoRepo(string url, string name)
        {
            _url = url;
            _name = name;

            using var client = new WebClient();
            var serverFileJson = client.DownloadString(_url);
            var serverFile = JsonConvert.DeserializeObject<ServerFile>(serverFileJson);

            var parts = _url.Split('/');
            parts[^1] = "";
            var baseUrl = string.Join('/', parts);
            _mods = serverFile.ModFolders.Select(m => (IRemoteMod)new BsoRepoMod(baseUrl + m.ModName, m.ModName)).ToList();
        }

        public List<IRemoteMod> GetMods()
        {
            return _mods;
        }

        public string GetName() => _name;

        public string GetLocation() => _url;
    }
}
