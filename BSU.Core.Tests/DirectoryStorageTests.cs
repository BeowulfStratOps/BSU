using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using BSU.Core.Model.Actions;
using Xunit;

namespace BSU.Core.Tests
{
    // TODO: those should really just thest the DirectoryStorage(Mod) class. NOTHING ELSE!!
    public class DirectoryStorageTests : IDisposable
    {
        private DirectoryInfo _tmpDir;

        public DirectoryStorageTests()
        {
            _tmpDir = new DirectoryInfo(Path.GetTempPath()).CreateSubdirectory(Guid.NewGuid().ToString());
        }

        public void Dispose()
        {
            _tmpDir.Delete(true);
        }

        private Dictionary<string, string> GetFileList()
        {
            return new Dictionary<string, string>
            {
                {"1.pbo", "42"},
                {"2.pbo", "<put shakespear quote here>"},
                {"3.pbo", "This statement is false."},
                {"mod.cpp", "name=\"ey yo\""}
            };
        }

        [Fact]
        private void Download()
        {
            var settings = new MockSettings();
            var core = new Core(settings, a => a());
            core.Types.AddRepoType("MOCK", url => new MockRepo());
            core.Model.AddRepository("MOCK", "url/test_repo", "test_repo");
            var repo = core.Model.Repositories.Single().Implementation as MockRepo;
            var repoMod = new MockRepositoryMod {Identifier = "repo_test"};
            repo.Mods.Add("repo_test", repoMod);
            core.Model.AddStorage("DIRECTORY", _tmpDir, "test_storage");

            foreach (var fileName in GetFileList().Keys)
            {
                repoMod.Files["/" + fileName] = Encoding.UTF8.GetBytes(GetFileList()[fileName]);
            }

            var state = core.Model;

            var selectedAction = state.Repositories.Single().Mods.Single().Actions.Values.OfType<DownloadAction>().Single();
            selectedAction.FolderName = "downloaded";
            var update = state.PrepareUpdate(new List<ModAction>{selectedAction});
            state.DoUpdate(update);
            while (!update.IsDone())
            {
                Thread.Sleep(10);
            }

            Assert.False(update.HasError());

            Assert.False(File.Exists(Path.Combine(_tmpDir.FullName, "@downloaded", "x.dll")));
            foreach (var fileName in GetFileList().Keys)
            {
                Assert.Equal(GetFileList()[fileName],
                    File.ReadAllText(Path.Combine(_tmpDir.FullName, "@downloaded", fileName)));
            }
        }


        [Fact]
        private void Update()
        {
            var settings = new MockSettings();
            var core = new Core(settings, a => a());
            core.Types.AddRepoType("MOCK", url => new MockRepo());
            core.Model.AddRepository("MOCK", "url/test_repo", "test_repo");
            var repo = core.Model.Repositories.Single().Implementation as MockRepo;
            var repoMod = new MockRepositoryMod {Identifier = "repo_test"};
            repo.Mods.Add("repo_test", repoMod);
            core.Model.AddStorage("DIRECTORY", _tmpDir, "test_storage");

            var mod = Directory.CreateDirectory(Path.Combine(_tmpDir.FullName, "@my_mod"));
            File.WriteAllText(Path.Combine(mod.FullName, "1.pbo"), "some_worthless_stuff");
            File.WriteAllText(Path.Combine(mod.FullName, "2.pbo"), "whatever");
            File.WriteAllText(Path.Combine(mod.FullName, "x.dll"), "format c");
            File.WriteAllText(Path.Combine(mod.FullName, "mod.cpp"), "name=\"ey up\"");

            foreach (var fileName in GetFileList().Keys)
            {
                repoMod.Files["/" + fileName] = Encoding.UTF8.GetBytes(GetFileList()[fileName]);
            }

            var state = core.Model;

            var selectedAction = state.Repositories.Single().Mods.Single().Actions.Values.OfType<UpdateAction>().Single();
            var update = state.PrepareUpdate(new List<ModAction>{selectedAction});
            state.DoUpdate(update);
            while (!update.IsDone())
            {
                Thread.Sleep(10);
            }

            Assert.False(update.HasError());

            Assert.False(File.Exists(Path.Combine(mod.FullName, "x.dll")));
            foreach (var fileName in GetFileList().Keys)
            {
                Assert.Equal(GetFileList()[fileName], File.ReadAllText(Path.Combine(mod.FullName, fileName)));
            }
        }

        [Fact]
        private void CaseInsensitiveUpdate()
        {
            var settings = new MockSettings();
            var core = new Core(settings, a => a());
            core.Types.AddRepoType("MOCK", url => new MockRepo());
            core.Model.AddRepository("MOCK", "url/test_repo", "test_repo");
            var repo = core.Model.Repositories.Single().Implementation as MockRepo;
            var repoMod = new MockRepositoryMod {Identifier = "repo_test"};
            repo.Mods.Add("repo_test", repoMod);
            core.Model.AddStorage("DIRECTORY", _tmpDir, "test_storage");

            var mod = Directory.CreateDirectory(Path.Combine(_tmpDir.FullName, "@my_mod"));
            File.WriteAllText(Path.Combine(mod.FullName, "1.Pbo"), "some_worthless_stuff");
            File.WriteAllText(Path.Combine(mod.FullName, "2.pbo"), "whatever");
            File.WriteAllText(Path.Combine(mod.FullName, "X.dll"), "format c");
            File.WriteAllText(Path.Combine(mod.FullName, "MOD.cpp"), "name=\"ey up\"");

            foreach (var fileName in GetFileList().Keys)
            {
                repoMod.Files["/" + fileName] = Encoding.UTF8.GetBytes(GetFileList()[fileName]);
            }

            var state = core.Model;

            var selectedAction = state.Repositories.Single().Mods.Single().Actions.Values.OfType<UpdateAction>().Single();
            
            var update = state.PrepareUpdate(new List<ModAction>{selectedAction});
            state.DoUpdate(update);
            while (!update.IsDone())
            {
                Thread.Sleep(10);
            }

            Assert.False(update.HasError());

            Assert.False(File.Exists(Path.Combine(mod.FullName, "x.dll")));
            foreach (var fileName in GetFileList().Keys)
            {
                Assert.Equal(GetFileList()[fileName], File.ReadAllText(Path.Combine(mod.FullName, fileName)));
            }
        }
    }
}
