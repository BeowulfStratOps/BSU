using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BSU.Core.JobManager;
using BSU.Core.Model;
using BSU.Core.Model.Actions;
using BSU.Core.Sync;
using BSU.CoreCommon;
using Xunit;
using DownloadAction = BSU.Core.Model.Actions.DownloadAction;
using UpdateAction = BSU.Core.Model.Actions.UpdateAction;

namespace BSU.Core.Tests
{
    // TODO: extend for ViewState
    public class CoreStateStories
    {
        public CoreStateStories()
        {
        }

        private Repository AddRepo(Core core, string name)
        {
            core.Model.AddRepository("MOCK", "url/" + name, name);
            return core.Model.Repositories.Single();
        }

        private Model.Storage AddStorage(Core core, string name)
        {
            core.Model.AddStorage("MOCK", new DirectoryInfo("path/" + name), name);
            return core.Model.Storages.Single();
        }

        private string GetVersionHash(string version)
        {
            var mod = new MockRepositoryMod();
            mod.SetFile("Common2", "common2");
            mod.SetFile("Common1", "common1");
            mod.SetFile("Version", version);
            return new Hashes.VersionHash(mod).GetHashString();
        }
        
        private (Core, MockSettings, MockRepo, MockRepositoryMod, MockStorageMod, MockStorage) DoSetup2()
        {
            var settings = new MockSettings();
            var syncManager = new MockJobManager();
            var core = new Core(settings, syncManager, a => a());
            core.Types.AddRepoType("MOCK", url => new MockRepo(url));
            core.Types.AddStorageType("MOCK", path => new MockStorage(path));
            var repo = AddRepo(core, "test_repo");
            var repoMod = new RepositoryMod(repo, new MockRepositoryMod(), "test_repo_mod");
            repo.Mods.Add(repoMod);
            var storage = AddStorage(core, "test_storage");
            var storageMod = new StorageMod(storage, new MockStorageMod
            {
                Identifier = "test_storage_mod",
                Storage = storage.Implementation as MockStorage
            }, "test_storage_mod");
            storage.Mods.Add(storageMod);
            (repoMod.Implementation as MockRepositoryMod).SetFile("Common1", "common1");
            (repoMod.Implementation as MockRepositoryMod).SetFile("Common2", "common2");
            (storageMod.Implementation as MockStorageMod).SetFile("Common1", "common1");
            (storageMod.Implementation as MockStorageMod).SetFile("Common2", "common2");
            return (core, settings, repo.Implementation as MockRepo, repoMod.Implementation as MockRepositoryMod, 
                storageMod.Implementation as MockStorageMod, storage.Implementation as MockStorage);
        }

        private (Core, MockSettings, MockRepo, MockRepositoryMod, MockStorageMod, MockStorage) DoSetup(IJobManager syncManager)
        {
            var settings = new MockSettings();
            var core = new Core(settings, syncManager, a => a());
            core.Types.AddRepoType("MOCK", url => new MockRepo(url));
            core.Types.AddStorageType("MOCK", path => new MockStorage(path));
            var repo = AddRepo(core, "test_repo");
            var repoMod = new RepositoryMod(repo, new MockRepositoryMod(), "test_repo_mod");
            repo.Mods.Add(repoMod);
            var storage = AddStorage(core, "test_storage");
            var storageMod = new StorageMod(storage, new MockStorageMod
            {
                Identifier = "test_storage_mod",
                Storage = storage.Implementation as MockStorage
            }, "test_storage_mod");
            storage.Mods.Add(storageMod);
            (repoMod.Implementation as MockRepositoryMod).SetFile("Common1", "common1");
            (repoMod.Implementation as MockRepositoryMod).SetFile("Common2", "common2");
            (storageMod.Implementation as MockStorageMod).SetFile("Common1", "common1");
            (storageMod.Implementation as MockStorageMod).SetFile("Common2", "common2");
            return (core, settings, repo.Implementation as MockRepo, repoMod.Implementation as MockRepositoryMod, 
                storageMod.Implementation as MockStorageMod, storage.Implementation as MockStorage);
        }

        [Theory]
        [ClassData(typeof(StateTestData))]
        private void CheckState(string repoModVer, string storageModVer, string updatingTo, string job)
        {
            var syncManager = new MockJobManager();
            var (core, settings, repo, repoMod, storageMod, storage) = DoSetup(syncManager);
            SetJob(job, core, storageMod, repoMod);
            SetRepoMod(repoModVer, repo, repoMod);
            SetStorageMod(storageModVer, storage, storageMod);
            SetUpdating(updatingTo, core, storage, storageMod);

            if (storageModVer == "" && updatingTo != "") updatingTo = "";

            var shouldFail = job != "" && (updatingTo != job || repoModVer != job || storageModVer == "");


            var model = core.Model;
            core.Load();
            syncManager.DoWork();
            
            
            if (storageModVer == updatingTo && job == "") updatingTo = "";

            CheckSettings(settings, updatingTo);
            CheckJob(core, job);
            var actions = model.Repositories.First().Mods.SelectMany(m => m.Actions).Select(kv => kv.Value).ToList();
            if (repoModVer != "")
            {
                CheckDownload(repoModVer, actions, model.Storages.Single());
                CheckUse(repoModVer, storageModVer, updatingTo, job, actions,
                    model.Storages.Single().Mods.SingleOrDefault());
                CheckUpdate(repoModVer, storageModVer, updatingTo, job, actions,
                    model.Storages.Single().Mods.SingleOrDefault(),
                    model.Repositories.Single().Mods.Single());
                CheckAwaitUpdate(repoModVer, job, actions, model.Storages.Single().Mods.SingleOrDefault());
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
            var job = core.JobManager.GetAllJobs().OfType<RepoSync>().SingleOrDefault();
            if (jobVer == "") Assert.Null(job);
            else Assert.Equal(GetVersionHash(jobVer), job?.GetTargetHash());
        }

        private void SetJob(string jobVersion, Core core, IStorageMod storage, IRepositoryMod repository)
        {
            if (jobVersion == "") return;
            var repo = core.Model.Repositories.SelectMany(r => r.Mods).Single(m => m.Implementation == repository);
            var sto = core.Model.Storages.SelectMany(r => r.Mods).Single(m => m.Implementation == storage);
            core.JobManager.QueueJob(new RepoSync(repo, sto,
                new UpdateTarget(GetVersionHash(jobVersion), null), "set job", 0));
        }

        private void CheckDownload(string repoModVer, List<ModAction> actions, Model.Storage storage)
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

        private void SetUpdating(string version, Core core, MockStorage storage, MockStorageMod mod)
        {
            if (version == "") return;
            var target = new UpdateTarget(GetVersionHash(version), version);
            core.Model.Storages.Single(s => s.Implementation == storage).Mods.Single(m => m.Implementation == mod)
                .UpdateTarget = target;
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