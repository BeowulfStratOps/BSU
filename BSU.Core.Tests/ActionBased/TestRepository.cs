using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BSU.CoreCommon;

namespace BSU.Core.Tests.ActionBased;

internal class TestRepository : IRepository
{
    private readonly TaskCompletionSource _load = new();
    private Dictionary<string, IRepositoryMod> _mods = null!;

    public void Load(Dictionary<string, IRepositoryMod> mods)
    {
        _mods = mods;
        _load.SetResult();
    }

    public async Task<Dictionary<string, IRepositoryMod>> GetMods(CancellationToken cancellationToken)
    {
        await _load.Task;
        throw new System.NotImplementedException();
    }

    public async Task<ServerInfo> GetServerInfo(CancellationToken cancellationToken)
    {
        await _load.Task;
        throw new System.NotImplementedException();
    }
}
