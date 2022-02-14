using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BSU.CoreCommon;
using BSU.Hashes;

namespace BSU.Core.Tests.ActionBased.TestModel;

internal class TestRepositoryMod : IRepositoryMod
{
    private readonly TestModelInterface _testModelInterface;
    private readonly TaskCompletionSource _loadTcs = new();
    public Dictionary<string, byte[]> Files = null!;
    private readonly TaskCompletionSource _updateTcs = new();

    public TestRepositoryMod(TestModelInterface testModelInterface)
    {
        _testModelInterface = testModelInterface;
    }

    public void Load(Dictionary<string, byte[]> files)
    {
        _testModelInterface.DoInModelThread(() =>
        {
            Files = files;
            _loadTcs.SetResult();
        }, true);
    }

    public void FinishUpdate()
    {
        _testModelInterface.DoInModelThread(() => _updateTcs.SetResult(), false);
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
        return await SHA1AndPboHash.BuildAsync(memStream, ".pbo", cancellationToken);
    }

    public Task<byte[]> GetFile(string path, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
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
        await _updateTcs.Task;
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
}
