using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BSU.CoreCommon;
using BSU.CoreCommon.Hashes;
using BSU.Hashes;

namespace BSU.Core.Tests.ViewModelIntegration.TestModel;

internal class TestStorageMod : IStorageMod
{
    private readonly TaskCompletionSource _loadTcs = new();
    private readonly object _fileLock = new();
    public Dictionary<string, byte[]> Files = new();

    public void Load(Dictionary<string, byte[]> files)
    {
        Files = files;
        _loadTcs.SetResult();
    }

    public async Task<Stream> OpenWrite(string path, CancellationToken cancellationToken)
    {
        await _loadTcs.Task;
        lock (_fileLock)
        {
            if (!Files.ContainsKey(path))
                Files[path] = Array.Empty<byte>();
        }
        return new WriteAfterDisposeMemoryStream(data => Files[path] = data);
    }

    public async Task<Stream?> OpenRead(string path, CancellationToken cancellationToken)
    {
        await _loadTcs.Task;
        return Files.TryGetValue(path, out var data) ? new MemoryStream(data) : null;
    }

    public Task Move(string @from, string to, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<bool> HasFile(string path, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task Delete(string path, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public async Task<List<string>> GetFileList(CancellationToken cancellationToken)
    {
        await _loadTcs.Task;
        return Files.Keys.ToList();
    }

    public Task<FileHash> GetFileHash(string path, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public async Task<string> GetTitle(CancellationToken cancellationToken)
    {
        await _loadTcs.Task;
        return "";
    }

    public string Path { get; } = null!;

    public Dictionary<Type, Func<CancellationToken, Task<IModHash>>> GetHashFunctions() => new()
    {
        { typeof(VersionHash), async ct => await VersionHash.CreateAsync(this, ct) },
        { typeof(MatchHash), async ct => await MatchHash.CreateAsync(this, ct) }
    };
}

internal class WriteAfterDisposeMemoryStream : MemoryStream
{
    private readonly Action<byte[]> _save;

    public WriteAfterDisposeMemoryStream(Action<byte[]> save)
    {
        _save = save;
    }

    protected override void Dispose(bool disposing)
    {
        _save(ToArray());
        base.Dispose(disposing);
    }
}
