using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BSU.Core.Storage;
using Microsoft.VisualBasic;
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
        private async Task GetMods()
        {
            using var file = Create("@ace", "mod.cpp");
            file.WriteLine("Ey yo");
            var storage = new DirectoryStorage(_tmpDir.FullName);
            var mods = await storage.GetMods(CancellationToken.None);
            // TODO: do some checking
        }

        [Fact]
        private async Task CreateNestedFile()
        {
            using var file = Create("@ace", "mod.cpp");
            file.WriteLine("Ey yo");
            var storage = new DirectoryStorage(_tmpDir.FullName);
            var mods = await storage.GetMods(CancellationToken.None);
            using var newFile = await (mods.Values.Single().OpenFile("/addons/addon2.pbo", FileAccess.Write, CancellationToken.None));
            newFile.Write(new byte[] {1, 2, 3}, 0, 3);
            newFile.Close();
            // TODO: check file contents, file still in use
        }

        [Fact]
        private async Task DontCreateFileWhenReading()
        {
            Create("@ace", "some_other_file").Dispose();
            var storage = new DirectoryStorage(_tmpDir.FullName);
            var mods = await storage.GetMods(CancellationToken.None);
            mods.Values.Single().OpenFile("/mod.cpp", FileAccess.Read, CancellationToken.None);
            Assert.False(File.Exists(Path.Join(_tmpDir.FullName, "@ace", "mod.cpp")));
        }
    }
}
