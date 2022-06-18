using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BSU.CoreCommon;

namespace BSU.Core.Tests.ActionBased.TestModel;

internal class TestStorage : IStorage
{
    private readonly TaskCompletionSource _loadTcs = new();
    private Dictionary<string, IStorageMod> _mods = null!;
    private readonly bool _canWrite;

    public TestStorageMod GetMod(string modName) => (TestStorageMod)_mods[modName];

    public TestStorage(bool canWrite)
    {
        _canWrite = canWrite;
    }

    public void Load(params string[] mods)
    {
        var modsDict = new Dictionary<string, IStorageMod>();
        foreach (var modName in mods)
        {
            var mod = new TestStorageMod();
            modsDict.Add(modName, mod);
        }
        _mods = modsDict;
        _loadTcs.SetResult();
    }

    public void LoadEmpty()
    {
        _mods = new Dictionary<string, IStorageMod>();
        _loadTcs.SetResult();
    }

    public void Load(Exception exception)
    {
        _loadTcs.SetException(exception);
    }

    public bool CanWrite() => _canWrite;

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
        throw new NotImplementedException();
    }

    public string Location() => "";
}
