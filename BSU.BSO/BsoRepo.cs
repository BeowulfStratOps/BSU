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
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private readonly string _url;
        private Dictionary<string, IRepositoryMod> _mods;

        private readonly Uid _uid = new Uid();

        public BsoRepo(string url)
        {
            _url = url;
        }

        public void Load()
        {
            using var client = new WebClient();
            Logger.Debug("{0] Downloading server file from {1}", _uid, _url);
            var serverFileJson = client.DownloadString(_url);
            var serverFile = JsonConvert.DeserializeObject<ServerFile>(serverFileJson);

            var parts = _url.Split('/');
            parts[^1] = "";
            var baseUrl = string.Join('/', parts);
            _mods = serverFile.ModFolders.ToDictionary(m => m.ModName,
                m => (IRepositoryMod) new BsoRepoMod(baseUrl + m.ModName));
#if SlowMode
            Thread.Sleep(1337);
#endif
        }

        public Dictionary<string, IRepositoryMod> GetMods()
        {
            return _mods;
        }

        public string GetLocation() => _url;
        public Uid GetUid() => _uid;
    }
}
