using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BSU.Core.Storage;
using BSU.CoreCommon;
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
            fi.Directory!.Create();
            return fi.CreateText();
        }

        [Fact]
        private async Task GetMods()
        {
            await using var file = Create("@ace", "mod.cpp");
            await file.WriteLineAsync("Ey yo");
            var storage = new DirectoryStorage(_tmpDir.FullName, new TestJobManager());

            var mods = await storage.GetMods(CancellationToken.None);
            Assert.Single(mods);
            Assert.Contains("@ace", (IDictionary<string, IStorageMod>)mods);
        }

        [Fact]
        private async Task CreateNestedFile()
        {
            await using var file = Create("@ace", "mod.cpp");
            await file.WriteLineAsync("Ey yo");
            var storage = new DirectoryStorage(_tmpDir.FullName, new TestJobManager());
            var mods = await storage.GetMods(CancellationToken.None);
            await using var newFile = await (mods.Values.Single().OpenWrite("/addons/addon2.pbo", CancellationToken.None));
            await newFile.WriteAsync(new byte[] { 1, 2, 3 });
            newFile.Close();
            // TODO: check file contents, file still in use
        }

        [Fact]
        private async Task DontCreateFileWhenReading()
        {
            await Create("@ace", "some_other_file").DisposeAsync();
            var storage = new DirectoryStorage(_tmpDir.FullName, new TestJobManager());
            var mods = await storage.GetMods(CancellationToken.None);
            await mods.Values.Single().OpenRead("/mod.cpp", CancellationToken.None);
            Assert.False(File.Exists(Path.Join(_tmpDir.FullName, "@ace", "mod.cpp")));
        }
    }
}
