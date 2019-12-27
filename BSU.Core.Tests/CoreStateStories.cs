using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BSU.Core.State;
using BSU.Core.Sync;
using BSU.CoreCommon;
using Xunit;
using DownloadAction = BSU.Core.State.DownloadAction;
using UpdateAction = BSU.Core.State.UpdateAction;

namespace BSU.Core.Tests
{
    public class CoreStateStories
    {
        public CoreStateStories()
        {
        }

        private MockRepo AddRepo(Core core, string name)
        {
            core.AddRepo(name, "url/" + name, "MOCK");
            return core.State.GetRepositories().Single() as MockRepo;
        }

        private MockStorage AddStorage(Core core, string name)
        {
            core.AddStorage(name, new DirectoryInfo("path/" + name), "MOCK");
            return core.State.GetStorages().Single() as MockStorage;
        }

        private (Core, MockSettings, MockRepo, MockRepositoryMod, MockStorageMod, MockStorage) DoSetup()
        {
            var settings = new MockSettings();
            var syncManager = new MockSyncManager();
            var core = new Core(settings, syncManager);
            core.AddRepoType("MOCK", (name, url) => new MockRepo(name, url));
            core.AddStorageType("MOCK", (name, path) => new MockStorage(name, path));
            var repo = AddRepo(core, "test_repo");
            var repoMod = new MockRepositoryMod();
            repo.Mods.Add(repoMod);
            var storage = AddStorage(core, "test_storage");
            var storageMod = new MockStorageMod
            {
                Identifier = "test_storage_mod",
                Storage = storage
            };
            storage.Mods.Add(storageMod);
            repoMod.SetFile("Common1", "common1");
            repoMod.SetFile("Common2", "common2");
            storageMod.SetFile("Common1", "common1");
            storageMod.SetFile("Common2", "common2");
            return (core, settings, repo, repoMod, storageMod, storage);
        }

        private string GetVersionHash(string version)
        {
            var mod = new MockRepositoryMod();
            mod.SetFile("Common2", "common2");
            mod.SetFile("Common1", "common1");
            mod.SetFile("Version", version);
            return new Hashes.VersionHash(mod).GetHashString();
        }

        [Theory]
        [ClassData(typeof(StateTestData))]
        private void CheckState(string repoModVer, string storageModVer, string updatingTo, string job)
        {
            var (core, settings, repo, repoMod, storageMod, storage) = DoSetup();
            SetJob(job, core, storageMod, repoMod);
            SetRepoMod(repoModVer, repo, repoMod);
            SetStorageMod(storageModVer, storage, storageMod);
            SetUpdating(updatingTo, settings, storage, storageMod);

            if (storageModVer == "" && updatingTo != "") updatingTo = "";

            var shouldFail = job != "" && (updatingTo != job || repoModVer != job || storageModVer == "");

            State.State state;

            try
            {
                state = core.GetState();
                Assert.False(shouldFail, "Should have failed");
            }
            catch (InvalidOperationException)
            {
                if (shouldFail) return;
                throw;
            }

            if (storageModVer == updatingTo && job == "") updatingTo = "";

            CheckSettings(settings, updatingTo);
            CheckJob(core, job);
            var actions = state.Repos.First().Mods.SelectMany(m => m.Actions).ToList();
            if (repoModVer != "")
            {
                CheckDownload(repoModVer, actions, state.Storages.Single());
                CheckUse(repoModVer, storageModVer, updatingTo, job, actions,
                    state.Storages.Single().Mods.SingleOrDefault());
                CheckUpdate(repoModVer, storageModVer, updatingTo, job, actions,
                    state.Storages.Single().Mods.SingleOrDefault(),
                    state.Repos.Single().Mods.Single());
                CheckAwaitUpdate(repoModVer, job, actions, state.Storages.Single().Mods.SingleOrDefault());
            }

            Assert.Empty(actions);
        }

        private void CheckSettings(ISettings settings, string updatingToVer)
        {
            var updatingTo = settings.Storages.Single().Updating.Values.SingleOrDefault()?.Hash;
            if (updatingToVer == "") Assert.Null(updatingTo);
            else Assert.Equal(GetVersionHash(updatingToVer), updatingTo);
        }

        private void CheckJob(Core core, string jobVer)
        {
            var job = core.GetAllJobs().SingleOrDefault();
            if (jobVer == "") Assert.Null(job);
            else Assert.Equal(GetVersionHash(jobVer), job.GetTargetHash());
        }

        private void SetJob(string jobVersion, Core core, IStorageMod storage, IRepositoryMod repository)
        {
            if (jobVersion == "") return;
            core.SyncManager.QueueJob(new RepoSync(repository, storage,
                new UpdateTarget(GetVersionHash(jobVersion), null)));
        }

        private void CheckDownload(string repoModVer, List<ModAction> actions, State.Storage storage)
        {
            var download = actions.OfType<DownloadAction>().SingleOrDefault();
            Assert.NotNull(download);
            actions.Remove(download);
            Assert.Equal(download.Storage, storage);
        }

        private void CheckUse(string repoModVer, string storageModVer, string updateTo, string job,
            List<ModAction> actions, StorageMod mod)
        {
            if (repoModVer != storageModVer) return;
            if (updateTo != "") return;
            if (job != "") return;
            var use = actions.OfType<UseAction>().SingleOrDefault();
            Assert.NotNull(use);
            actions.Remove(use);
            Assert.Equal(use.StorageMod, mod);
        }

        private void CheckUpdate(string repoModVer, string storageModVer, string updateTo, string job,
            List<ModAction> actions, StorageMod storageMod, RepositoryMod repoMod)
        {
            if (storageModVer == "") return;
            if (job != "") return;
            if (repoModVer == storageModVer && updateTo == "") return;
            var update = actions.OfType<UpdateAction>().SingleOrDefault();
            Assert.NotNull(update);
            actions.Remove(update);
            Assert.Equal(update.IsContinuation, updateTo == repoModVer);
            Assert.Equal(update.UpdateTarget.Hash, GetVersionHash(repoModVer));
            Assert.Equal(update.StorageMod, storageMod);
            Assert.Equal(update.RepositoryMod, repoMod);
        }

        private void CheckAwaitUpdate(string repoModVer, string job, List<ModAction> actions, StorageMod storageMod)
        {
            if (job == "") return;
            if (repoModVer != job) return;
            var update = actions.OfType<AwaitUpdateAction>().SingleOrDefault();
            Assert.NotNull(update);
            actions.Remove(update);
            Assert.Equal(update.UpdateTarget.Hash, GetVersionHash(repoModVer));
            Assert.Equal(update.StorageMod, storageMod);
        }

        private void SetUpdating(string version, MockSettings settings, MockStorage storage, MockStorageMod mod)
        {
            if (version == "") return;
            var target = new UpdateTarget(GetVersionHash(version), version);
            settings.Storages.Single(s => s.Name == storage.GetIdentifier()).Updating[mod.GetIdentifier()] = target;
        }

        private void SetStorageMod(string version, MockStorage storage, MockStorageMod mod)
        {
            if (version == "")
            {
                storage.Mods.Clear();
                return;
            }

            mod.SetFile("Version", version);
        }

        private void SetRepoMod(string version, MockRepo repo, MockRepositoryMod mod)
        {
            if (version == "")
            {
                repo.Mods.Clear();
                return;
            }

            mod.SetFile("Version", version);
        }
    }

    class StateTestData : IEnumerable<object[]>
    {
        public IEnumerator<object[]> GetEnumerator()
        {
            foreach (var repoModVer in new[] {"", "v1", "v2"})
            {
                foreach (var storageModVer in new[] {"", "v1", "v2", "broken"})
                {
                    foreach (var updatingTo in new[] {"", "v1", "v2"})
                    {
                        foreach (var job in new[] {"", "v1", "v2"})
                        {
                            yield return new object[] {repoModVer, storageModVer, updatingTo, job};
                        }
                    }
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}