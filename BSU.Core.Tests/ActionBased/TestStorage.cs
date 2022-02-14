using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BSU.CoreCommon;

namespace BSU.Core.Tests.ActionBased;

internal class TestStorage : IStorage
{
    private readonly TaskCompletionSource _loadTcs = new();
    private Dictionary<string, IStorageMod> _mods = null!;

    public TestStorageMod GetMod(string modName) => (TestStorageMod)_mods[modName];

    public void Load(Dictionary<string, IStorageMod> mods)
    {
        _mods = mods;
        _loadTcs.SetResult();
    }

    public bool CanWrite() => true;

    public async Task<Dictionary<string, IStorageMod>> GetMods(CancellationToken cancellationToken)
    {
        await _loadTcs.Task;
        return _mods;
    }

    public async Task<IStorageMod> CreateMod(string identifier, CancellationToken cancellationToken)
    {
        await _loadTcs.Task;
        var mod = new TestStorageMod();
        _mods.Add(identifier, mod);
        return mod;
    }

    public Task RemoveMod(string identifier, CancellationToken cancellationToken)
    {
        throw new System.NotImplementedException();
    }
}
