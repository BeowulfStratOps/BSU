using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BSU.CoreCommon;
using BSU.Hashes;

namespace BSU.Core.Tests.ActionBased;

internal class TestStorageMod : IStorageMod
{
    private readonly TaskCompletionSource _loadTcs = new();
    public Dictionary<string, byte[]> Files = new();

    public void Load(Dictionary<string, byte[]> files)
    {
        Files = files;
        _loadTcs.SetResult();
    }

    public async Task<Stream> OpenWrite(string path, CancellationToken cancellationToken)
    {
        await _loadTcs.Task;
        if (!Files.ContainsKey(path))
            Files[path] = Array.Empty<byte>();
        return new WriteAfterDisposeMemoryStream(data => Files[path] = data);
    }

    public Task<Stream?> OpenRead(string path, CancellationToken cancellationToken)
    {
        throw new System.NotImplementedException();
    }

    public Task Move(string @from, string to, CancellationToken cancellationToken)
    {
        throw new System.NotImplementedException();
    }

    public Task<bool> HasFile(string path, CancellationToken cancellationToken)
    {
        throw new System.NotImplementedException();
    }

    public Task Delete(string path, CancellationToken cancellationToken)
    {
        throw new System.NotImplementedException();
    }

    public async Task<List<string>> GetFileList(CancellationToken cancellationToken)
    {
        await _loadTcs.Task;
        return Files.Keys.ToList();
    }

    public Task<FileHash> GetFileHash(string path, CancellationToken cancellationToken)
    {
        throw new System.NotImplementedException();
    }

    public Task<string> GetTitle(CancellationToken cancellationToken)
    {
        throw new System.NotImplementedException();
    }

    public string Path { get; } = null!;
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
