using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using BSU.BSO.FileStructures;
using BSU.CoreCommon;
using Newtonsoft.Json;
using NLog;

namespace BSU.BSO
{
    /// <summary>
    /// Internal representation of a repository in BSO format.
    /// </summary>
    public class BsoRepo : IRepository
    {
        private readonly Logger _logger = EntityLogger.GetLogger();

        private readonly string _url;
        private Dictionary<string, IRepositoryMod> _mods;

        public BsoRepo(string url)
        {
            _url = url;
        }

        public void Load()
        {
            using var client = new WebClient();
            _logger.Debug("Downloading server file from {0}", _url);
            var serverFileJson = client.DownloadString(_url);
            var serverFile = JsonConvert.DeserializeObject<ServerFile>(serverFileJson);

            var parts = _url.Split('/');
            parts[^1] = "";
            var baseUrl = string.Join('/', parts);
            _mods = serverFile.ModFolders.ToDictionary(m => m.ModName,
                m => (IRepositoryMod) new BsoRepoMod(baseUrl + m.ModName));
        }

        public Dictionary<string, IRepositoryMod> GetMods()
        {
            return _mods;
        }

        public string GetLocation() => _url;
    }
}
