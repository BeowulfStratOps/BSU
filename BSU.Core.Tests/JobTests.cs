using System;
using System.IO;
using System.Linq;
using System.Threading;
using BSU.Core.Sync;
using Xunit;
using UpdateAction = BSU.Core.State.UpdateAction;

namespace BSU.Core.Tests
{
    public class JobTests
    {
        private MockSettings _settings;
        private Core _core;
        private MockRepo _repo;
        private MockRepositoryMod _repoMod;
        private MockStorage _storage;
        private MockStorageMod _storageMod;

        public JobTests()
        {
            _settings = new MockSettings();
            _core = new Core(_settings);
            _core.AddRepoType("MOCK", (name, url) => new MockRepo(name, url));
            _core.AddStorageType("MOCK", (name, path) => new MockStorage(name, path));
            _repo = AddRepo("test_repo");
            _repoMod = new MockRepositoryMod();
            _repo.Mods.Add(_repoMod);
            _storage = AddStorage("test_storage");
            _storageMod = new MockStorageMod
            {
                Identifier = "test_storage_mod",
                Storage = _storage
            };
            _storage.Mods.Add(_storageMod);
            SetupFiles(_repoMod, "2");
            SetupFiles(_storageMod, "1");
        }

        private void SetupFiles(IMockedFiles mod, string version)
        {
            for (int i = 0; i < 100; i++)
            {
                mod.SetFile($"File{i}", version);
            }
        }

        private MockRepo AddRepo(string name)
        {
            _core.AddRepo(name, "url/" + name, "MOCK");
            return _core.State.GetRepositories().Single() as MockRepo;
        }

        private MockStorage AddStorage(string name)
        {
            _core.AddStorage(name, new DirectoryInfo("path/" + name), "MOCK");
            return _core.State.GetStorages().Single() as MockStorage;
        }

        [Fact]
        public void StateInvalidationTest()
        {
            var state = _core.GetState();
            Assert.True(state.IsValid);
            var stateRemoteMod = state.Repos.Single().Mods.Single();
            stateRemoteMod.Selected = stateRemoteMod.Actions.OfType<UpdateAction>().Single();

            state.Repos.Single().PrepareUpdate().DoUpdate();
            Assert.False(state.IsValid);

            while (_core.GetActiveJobs().Any())
            {
                Thread.Sleep(1);
            }

            Assert.False(state.IsValid);
        }

        private WeakReference CreateStateReference()
        {
            var state = _core.GetState();
            return new WeakReference(state);
        }

        [Fact(Skip = "Events will become much more important with the GUI. Re-visit at that point")]
        public void StateDisposeTest1()
        {
            _repoMod.SetFile("Version", "Version2");
            _storageMod.SetFile("Version", "Version1");

            var stateRef = CreateStateReference();
            GC.Collect();
            Assert.False(stateRef.IsAlive);
        }

        [Fact]
        public void AbortJobTest()
        {
            var state = _core.GetState();
            var stateRemoteMod = state.Repos.Single().Mods.Single();
            stateRemoteMod.Selected = stateRemoteMod.Actions.OfType<UpdateAction>().Single();

            _repoMod.NoOp = true;
            _repoMod.SleepMs = 50;

            state.Repos.Single().PrepareUpdate().DoUpdate();

            Thread.Sleep(150);
            Assert.Single(_core.GetActiveJobs());
            _core.GetActiveJobs().Single().Abort();

            Thread.Sleep(10);
            Assert.Empty(_core.GetActiveJobs());
            var syncState = _core.GetAllJobs().Single() as RepoSync;
            Assert.False(syncState.HasError());
            Assert.True(syncState.IsDone());
        }

        [Fact]
        public void GetStateWithJobTest()
        {
            var state = _core.GetState();
            var stateRemoteMod = state.Repos.Single().Mods.Single();
            stateRemoteMod.Selected = stateRemoteMod.Actions.OfType<UpdateAction>().Single();

            _repoMod.NoOp = true;
            _repoMod.SleepMs = 50;

            state.Repos.Single().PrepareUpdate().DoUpdate();
            Assert.False(state.IsValid);

            _storageMod.Locked = true;

            state = _core.GetState(); // would fail if it couldn't read file
        }

        [Fact]
        public void DeleteRepoWithJobTest()
        {
            var state = _core.GetState();
            var stateRemoteMod = state.Repos.Single().Mods.Single();
            stateRemoteMod.Selected = stateRemoteMod.Actions.OfType<UpdateAction>().Single();

            _repoMod.NoOp = true;
            _repoMod.SleepMs = 50;

            state.Repos.Single().PrepareUpdate().DoUpdate();

            Assert.Throws<InvalidOperationException>(() => { state.Repos.Single().Remove(); });
        }

        [Fact]
        public void DeleteStorageWithJobTest()
        {
            var state = _core.GetState();
            var stateRemoteMod = state.Repos.Single().Mods.Single();
            stateRemoteMod.Selected = stateRemoteMod.Actions.OfType<UpdateAction>().Single();

            _repoMod.NoOp = true;
            _repoMod.SleepMs = 50;

            state.Repos.Single().PrepareUpdate().DoUpdate();

            Assert.Throws<InvalidOperationException>(() => { state.Storages.Single().Remove(); });
        }
    }
}
