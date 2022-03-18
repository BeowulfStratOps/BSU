using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace BSU.Server.Tests;

public class MockSourceMod : ISourceMod
{
    public readonly Dictionary<NormalizedPath, byte[]> Files = new();
    public List<NormalizedPath> GetFileList() => Files.Keys.ToList();

    public Stream OpenRead(NormalizedPath path) => new MemoryStream(Files[path]);

    public DateTime GetLastChangeDateTime(NormalizedPath path) => DateTime.Now;
}
