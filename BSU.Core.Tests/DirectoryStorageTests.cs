using System;
using System.Collections.Generic;
using System.IO;
using BSU.Core.Storage;
using Xunit;

namespace BSU.Core.Tests
{
    public class DirectoryStorageTests : IDisposable
    {
        private readonly DirectoryInfo _tmpDir;

        public DirectoryStorageTests()
        {
            _tmpDir = new DirectoryInfo(Path.GetTempPath()).CreateSubdirectory(Guid.NewGuid().ToString());
        }

        public void Dispose()
        {
            _tmpDir.Delete(true);
        }

        private StreamWriter Create(params string[] path)
        {
            var absPath = Path.Combine(_tmpDir.FullName, Path.Combine(path));
            var fi = new FileInfo(absPath);
            fi.Directory.Create();
            return fi.CreateText();
        }

        [Fact]
        private void GetMods()
        {
            Create("@ace", "mod.cpp").WriteLine("Ey yo");
            var storage = new DirectoryStorage(_tmpDir.FullName);
            storage.Load();
            var mods = storage.GetMods();
        }
    }
}
