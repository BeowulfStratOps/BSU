using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BSU.CoreCommon.Hashes;

namespace BSU.Core.Hashes;

internal class HashManager
{
    private readonly Dictionary<Type, Func<CancellationToken, Task<IModHash>>> _hashFunctions = new();
    private readonly Dictionary<Type, Task<IModHash>> _hashTasks = new();
    private CancellationTokenSource _cts = new();

    public void AddHashFunction(Type type, Func<CancellationToken, Task<IModHash>> func)
    {
        _hashFunctions.Add(type, func);
    }

    public Task<IModHash> GetHash(Type type)
    {
        if (_hashTasks.TryGetValue(type, out var runningTask))
            return runningTask;

        if (!_hashFunctions.TryGetValue(type, out var hashFunc))
            throw new NotSupportedException();

        var task = hashFunc(_cts.Token);
        _hashTasks.Add(type, task);
        if (!task.IsCompleted)
            task.ContinueWith(_ => JobCompleted?.Invoke());
        return task;
    }

    public event Action? JobCompleted;

    public List<Type> GetSupportedTypes() => _hashFunctions.Keys.ToList();

    public async Task Reset(IEnumerable<IModHash> newHashes)
    {
        _cts.Cancel();
        _cts = new CancellationTokenSource();
        
        _hashFunctions.Clear();
        var runningTasks = _hashTasks.Values.ToList();
        _hashTasks.Clear();
        
        Set(newHashes);

        try
        {
            await Task.WhenAll(runningTasks);
        }
        catch (Exception)
        {
            // just waiting
        }
    }

    public void Set(IEnumerable<IModHash> hashes)
    {
        foreach (var hash in hashes)
        {
            AddHashFunction(hash.GetType(), _ => Task.FromResult(hash));
        }
    }
}
