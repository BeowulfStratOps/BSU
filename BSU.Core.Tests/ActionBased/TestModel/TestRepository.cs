using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BSU.CoreCommon;

namespace BSU.Core.Tests.ActionBased.TestModel;

internal class TestRepository : IRepository
{
    private readonly TaskCompletionSource _load = new();
    private Dictionary<string, IRepositoryMod> _mods = null!;

    public void Load(params string[] mods)
    {
        var modsDict = new Dictionary<string, IRepositoryMod>();
        foreach (var modName in mods)
        {
            var mod = new TestRepositoryMod();
            modsDict.Add(modName, mod);
        }
        _mods = modsDict;
        _load.SetResult();
    }

    public TestRepositoryMod GetMod(string modName) => (TestRepositoryMod)_mods[modName];

    public async Task<Dictionary<string, IRepositoryMod>> GetMods(CancellationToken cancellationToken)
    {
        await _load.Task;
        return _mods;
    }

    public async Task<ServerInfo> GetServerInfo(CancellationToken cancellationToken)
    {
        await _load.Task;
        return new ServerInfo("", "", 0, new List<ulong>());
    }
}
