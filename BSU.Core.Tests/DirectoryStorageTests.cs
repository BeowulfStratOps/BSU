using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
            // TODO: do some checking
        }

        [Fact]
        private void CreateNestedFile()
        {
            using var file = Create("@ace", "mod.cpp");
            file.WriteLine("Ey yo");
            var storage = new DirectoryStorage(_tmpDir.FullName);
            storage.Load();
            using var newFile = storage.GetMods().Values.Single().OpenFile("/addons/addon2.pbo", FileAccess.Write);
            newFile.Write(new byte[] {1, 2, 3}, 0, 3);
            newFile.Close();
            // TODO: check file contents, file still in use
        }
    }
}
