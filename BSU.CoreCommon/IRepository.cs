﻿using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BSU.CoreCommon
{
    /// <summary>
    /// Repository of mods. Likely on a remote server.
    /// </summary>
    public interface IRepository
    {
        Task<Dictionary<string, IRepositoryMod>> GetMods(CancellationToken cancellationToken);
        Task<ServerInfo> GetServerInfo(CancellationToken cancellationToken);
    }

    public record ServerInfo(string Name, string Url, ushort Port, List<ulong> Cdlcs);

    public record ServerUrlCheck(ServerInfo Server, string Type);

    public delegate Task<ServerUrlCheck?> RepositoryUrlChecker(string url, CancellationToken cancellationToken);
}
