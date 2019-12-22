using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using BSU.BSO.FileStructures;
using BSU.CoreCommon;
using Newtonsoft.Json;
using NLog;
using NLog.Fluent;

namespace BSU.BSO
{
    public class BsoRepo : IRepository
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private readonly string _url, _name;
        private readonly List<IRepositoryMod> _mods;

        public BsoRepo(string url, string name)
        {
            _url = url;
            _name = name;

            using var client = new WebClient();
            Logger.Debug("Downloading server file from {0}", _url);
            var serverFileJson = client.DownloadString(_url);
            var serverFile = JsonConvert.DeserializeObject<ServerFile>(serverFileJson);

            var parts = _url.Split('/');
            parts[^1] = "";
            var baseUrl = string.Join('/', parts);
            _mods = serverFile.ModFolders.Select(m => (IRepositoryMod)new BsoRepoMod(baseUrl + m.ModName, m.ModName)).ToList();
        }

        public List<IRepositoryMod> GetMods()
        {
            return _mods;
        }

        public string GetName() => _name;

        public string GetLocation() => _url;
    }
}
