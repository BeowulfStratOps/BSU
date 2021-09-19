using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
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
        private ServerFile _serverFile;

        public BsoRepo(string url)
        {
            _url = url;
        }

        private async Task Load(CancellationToken cancellationToken)
        {
            using var client = new HttpClient();
            _logger.Debug("Downloading server file from {0}", _url);
            var serverFileJson = await client.GetStringAsync(_url, cancellationToken);
            _serverFile = JsonConvert.DeserializeObject<ServerFile>(serverFileJson);

            var parts = _url.Split('/');
            parts[^1] = "";
            var baseUrl = string.Join('/', parts);
            _mods = _serverFile.ModFolders.ToDictionary(m => m.ModName,
                m => (IRepositoryMod) new BsoRepoMod(baseUrl + m.ModName));
        }

        public async Task<Dictionary<string, IRepositoryMod>> GetMods(CancellationToken cancellationToken)
        {
            await Load(cancellationToken);
            return _mods;
        }

        public async Task<ServerInfo> GetServerInfo(CancellationToken cancellationToken)
        {
            await Load(CancellationToken.None);
            return new ServerInfo(_serverFile.ServerName, _serverFile.ServerAddress);
        }
    }
}
