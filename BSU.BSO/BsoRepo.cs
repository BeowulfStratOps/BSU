using System.Collections.Generic;
using System.IO;
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
        public const string RepoType = "BSO";
        private readonly Logger _logger = LogManager.GetCurrentClassLogger();

        private readonly string _url;
        private Dictionary<string, IRepositoryMod>? _mods;
        private ServerFile? _serverFile;
        private readonly Task _loading;
        private readonly IJobManager _jobManager;

        public BsoRepo(string url, IJobManager jobManager)
        {
            _url = url;
            _loading = jobManager.Run($"Bso Repo Load {_url}", () => Load(CancellationToken.None), CancellationToken.None);
            _jobManager = jobManager;
        }

        private async Task Load(CancellationToken cancellationToken)
        {
            using var client = new HttpClient();
            _logger.Debug($"Downloading server file from {_url}");
            var serverFileJson = await client.GetStringAsync(_url, cancellationToken);
            _serverFile = JsonConvert.DeserializeObject<ServerFile>(serverFileJson) ?? throw new InvalidDataException();

            var parts = _url.Split('/');
            parts[^1] = "";
            var baseUrl = string.Join('/', parts);
            _mods = _serverFile.ModFolders.ToDictionary(m => m.ModName,
                m => (IRepositoryMod) new BsoRepoMod(baseUrl + m.ModName, m.Hash, _jobManager));
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
            var cdlcs = serverFile.Dlcs?.Select(ulong.Parse).ToList() ?? new List<ulong>();
            return new ServerInfo(serverFile.ServerName, serverFile.ServerAddress, serverFile.ServerPort, cdlcs);
        }

        public static async Task<ServerUrlCheck?> CheckUrl(string url, CancellationToken cancellationToken)
        {
            try
            {
                using var client = new HttpClient();
                var serverFileJson = await client.GetStringAsync(url, cancellationToken);
                var serverFile = JsonConvert.DeserializeObject<ServerFile>(serverFileJson) ?? throw new InvalidDataException();
                return new ServerUrlCheck(GetServerInfo(serverFile), RepoType);
            }
            catch
            {
                return null;
            }
        }
    }
}
