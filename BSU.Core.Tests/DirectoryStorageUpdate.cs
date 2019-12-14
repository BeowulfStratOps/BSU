using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using BSU.Core.State;
using Xunit;

namespace BSU.Core.Tests
{
    public class DirectoryStorageUpdate : IDisposable
    {
        private DirectoryInfo _tmpDir;
        public DirectoryStorageUpdate()
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
                {"1.pbo", "42" },
                {"2.pbo", "<put shakespear quote here>" },
                {"3.pbo", "This statement is false." },
                {"mod.cpp", "name=\"ey yo\"" }
            };
        }


        [Fact]
        private void Update()
        {
            var settings = new MockSettings();
            var core = new Core(settings);
            core.AddRepoType("MOCK", (name, url) => new MockRepo(name, url));
            core.AddRepo("test_repo", "url/test_repo", "MOCK");
            var repo = core.State.GetRepositories().Single() as MockRepo;
            var remoteMod = new MockRemoteMod { Identifier = "remote_test" };
            repo.Mods.Add(remoteMod);
            core.AddStorage("test_storage", _tmpDir, "DIRECTORY");

            var mod = Directory.CreateDirectory(Path.Combine(_tmpDir.FullName, "@my_mod"));
            File.WriteAllText(Path.Combine(mod.FullName, "1.pbo"), "some_worthless_stuff");
            File.WriteAllText(Path.Combine(mod.FullName, "2.pbo"), "whatever");
            File.WriteAllText(Path.Combine(mod.FullName, "x.dll"), "format c");
            File.WriteAllText(Path.Combine(mod.FullName, "mod.cpp"), "name=\"ey up\"");

            foreach (var fileName in GetFileList().Keys)
            {
                remoteMod.Files["/" + fileName] = Encoding.UTF8.GetBytes(GetFileList()[fileName]);
            }

            var state = core.GetState();

            var selectedAction = state.Repos.Single().Mods.Single().Actions.OfType<UpdateAction>().Single();
            state.Repos.Single().Mods.Single().Selected = selectedAction;

            var update = state.Repos.Single().PrepareUpdate();
            update.DoUpdate();
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
