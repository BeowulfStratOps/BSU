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

internal class TestRepositoryMod : IRepositoryMod
{
    private readonly TaskCompletionSource _loadTcs = new();
    public Dictionary<string, byte[]> Files = null!;
    private readonly List<TaskCompletionSource> _updateTcs = new();
    private readonly object _updateTcsLock = new();
    private bool _finishUpdateCalled;

    public void Load(Dictionary<string, byte[]> files)
    {
        Files = files;
        _loadTcs.SetResult();
    }

    public void FinishUpdate()
    {
        List<TaskCompletionSource> pending;
        lock (_updateTcsLock)
        {
            _finishUpdateCalled = true;
            pending = _updateTcs.ToList();
        }

        // complete currently known update units, and let any future ones complete immediately
        foreach (var tcs in pending)
        {
            tcs.SetResult();
        }
    }

    public async Task<List<string>> GetFileList(CancellationToken cancellationToken)
    {
        await _loadTcs.Task;
        return Files.Keys.ToList();
    }

    public async Task<FileHash> GetFileHash(string path, CancellationToken cancellationToken)
    {
        await _loadTcs.Task;
        await using var memStream = new MemoryStream(Files[path]);
        return await Sha1AndPboHash.BuildAsync(memStream, ".pbo", cancellationToken);
    }

    public async Task<byte[]> GetFile(string path, CancellationToken cancellationToken)
    {
        await _loadTcs.Task;
        if (!Files.TryGetValue(path, out var data)) throw new FileNotFoundException();
        return data;
    }

    public async Task<(string name, string version)> GetDisplayInfo(CancellationToken cancellationToken)
    {
        await _loadTcs.Task;
        return ("", "");
    }

    public async Task<ulong> GetFileSize(string path, CancellationToken cancellationToken)
    {
        await _loadTcs.Task;
        return (uint)Files[path].Length;
    }

    public async Task DownloadTo(string path, IFileSystem fileSystem, IProgress<ulong> progress, CancellationToken cancellationToken)
    {
        var tcs = new TaskCompletionSource();

        lock (_updateTcsLock)
        {
            _updateTcs.Add(tcs);
            if (_finishUpdateCalled)
                tcs.SetResult();
        }

        await tcs.Task;
        var data = Files[path];
        await using var stream = await fileSystem.OpenWrite(path, cancellationToken);
        stream.SetLength(0);
        await stream.WriteAsync(data, cancellationToken);
        progress.Report((ulong)data.Length);
    }

    public async Task UpdateTo(string path, IFileSystem fileSystem, IProgress<ulong> progress, CancellationToken cancellationToken)
    {
        await DownloadTo(path, fileSystem, progress, cancellationToken);
    }

    public async Task<HashCollection> GetHashes(CancellationToken cancellationToken) =>
        new(
            await MatchHash.CreateAsync(this, cancellationToken),
            await VersionHash.CreateAsync(this, cancellationToken)
        );
}
