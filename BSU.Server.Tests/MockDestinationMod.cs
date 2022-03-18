using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace BSU.Server.Tests;

public class MockDestinationMod : IDestinationMod
{
    public readonly Dictionary<NormalizedPath, byte[]> Files = new();
    public readonly HashSet<NormalizedPath> WrittenFiles = new();
    public readonly HashSet<NormalizedPath> RemovedFiles = new();

    public Dictionary<NormalizedPath, long> GetFileList() =>
        Files.ToDictionary(kv => kv.Key, kv => (long)kv.Value.Length);

    public Stream OpenRead(NormalizedPath path)
    {
        if (!Files.TryGetValue(path, out var data)) throw new KeyNotFoundException();
        return new NonSeekableStream(data);
    }

    public void Write(NormalizedPath path, Stream data)
    {
        if (WrittenFiles.Contains(path))
            throw new Exception($"{path} written more than once");
        WrittenFiles.Add(path);
        var memStream = new MemoryStream();
        data.CopyTo(memStream);
        Files[path] = memStream.ToArray();
    }

    public void Remove(NormalizedPath path)
    {
        if (!Files.ContainsKey(path))
            throw new KeyNotFoundException();
        Files.Remove(path);
        RemovedFiles.Add(path);
    }
}

public class NonSeekableStream : MemoryStream
{
    public NonSeekableStream(byte[] value) : base(value)
    {
    }

    public override bool CanSeek => false;

    public override long Position
    {
        get => base.Position;
        set => throw new NotSupportedException();
    }
}
