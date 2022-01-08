using System.Collections.Generic;
using System.Linq;
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
        private readonly Logger _logger = LogManager.GetCurrentClassLogger();

        private readonly string _url;
        private Dictionary<string, IRepositoryMod>? _mods;
        private ServerFile? _serverFile;
        private readonly Task _loading;

        public BsoRepo(string url)
        {
            _url = url;
            _loading = Task.Run(() => Load(CancellationToken.None));
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
            // TODO: use cancellationToken
            await _loading;
            return _mods!;
        }

        public async Task<ServerInfo> GetServerInfo(CancellationToken cancellationToken)
        {
            // TODO: use cancellationToken
            await _loading;
            return GetServerInfo(_serverFile!);
        }

        private static ServerInfo GetServerInfo(ServerFile serverFile)
        {
            var cdlcs = serverFile.DLCs.Select(ulong.Parse).ToList();
            return new ServerInfo(serverFile.ServerName, serverFile.ServerAddress, serverFile.ServerPort, cdlcs);
        }

        public static async Task<ServerInfo?> CheckUrl(string url, CancellationToken cancellationToken)
        {
            try
            {
                using var client = new HttpClient();
                var serverFileJson = await client.GetStringAsync(url, cancellationToken);
                var serverFile = JsonConvert.DeserializeObject<ServerFile>(serverFileJson);
                return GetServerInfo(serverFile);
            }
            catch
            {
                return null;
            }
        }
    }
}
