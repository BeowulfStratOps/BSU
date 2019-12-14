using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BSU.Core.State;
using BSU.CoreInterface;
using Xunit;

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

        private (Core, MockSettings, MockRepo, MockRemoteMod, MockStorageMod, MockStorage) DoSetup()
        {
            var settings = new MockSettings();
            var syncManager = new MockSyncManager();
            var core = new Core(settings, syncManager);
            core.AddRepoType("MOCK", (name, url) => new MockRepo(name, url));
            core.AddStorageType("MOCK", (name, path) => new MockStorage(name, path));
            var repo = AddRepo(core, "test_repo");
            var remoteMod = new MockRemoteMod();
            repo.Mods.Add(remoteMod);
            var storage = AddStorage(core, "test_storage");
            var localMod = new MockStorageMod
            {
                Identifier = "test_local_mod",
                Storage = storage
            };
            storage.Mods.Add(localMod);
            remoteMod.SetFile("Common1", "common1");
            remoteMod.SetFile("Common2", "common2");
            localMod.SetFile("Common1", "common1");
            localMod.SetFile("Common2", "common2");
            return (core, settings, repo, remoteMod, localMod, storage);
        }

        private string GetVersionHash(string version)
        {
            var mod = new MockRemoteMod();
            mod.SetFile("Common2", "common2");
            mod.SetFile("Common1", "common1");
            mod.SetFile("Version", version);
            return new Hashes.VersionHash(mod).GetHashString();
        }

        [Theory]
        [ClassData(typeof(StateTestData))]
        private void CheckState(string remoteVer, string localVer, string updatingTo, string job)
        {
            var (core, settings, repo, remoteMod, localMod, storage) = DoSetup();
            SetJob(job, core, localMod, remoteMod);
            SetRemote(remoteVer, repo, remoteMod);
            SetLocal(localVer, storage, localMod);
            SetUpdating(updatingTo, settings, storage, localMod);



            if (localVer == "" && updatingTo != "") updatingTo = "";
            if (localVer == updatingTo) updatingTo = "";

            var shouldFail = job != "" && (updatingTo != job || remoteVer != job || localVer == "");

            if (job != "" && job != remoteVer) job = "";


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

            CheckSettings(settings, updatingTo);
            CheckJob(core, job);
            var actions = state.Repos.First().Mods.SelectMany(m => m.Actions).ToList();
            if (remoteVer != "")
            {
                CheckDownload(remoteVer, actions, state.Storages.Single());
                CheckUse(remoteVer, localVer, updatingTo, job, actions, state.Storages.Single().Mods.SingleOrDefault());
                CheckUpdate(remoteVer, localVer, updatingTo, job, actions, state.Storages.Single().Mods.SingleOrDefault(),
                    state.Repos.Single().Mods.Single());
                CheckAwaitUpdate(remoteVer, job, actions, state.Storages.Single().Mods.SingleOrDefault());
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
            else Assert.Equal(GetVersionHash(jobVer), job.TargetHash);
        }

        private void SetJob(string jobVersion, Core core, ILocalMod local, IRemoteMod remote)
        {
            if (jobVersion == "") return;
            core.SyncManager.QueueJob(new UpdateJob(local, remote, new UpdateTarget(GetVersionHash(jobVersion), null), null));
        }

        private void CheckDownload(string remoteVer, List<ModAction> actions, Storage storage)
        {
            var download = actions.OfType<DownloadAction>().SingleOrDefault();
            Assert.NotNull(download);
            actions.Remove(download);
            Assert.Equal(download.Storage, storage);
        }

        private void CheckUse(string remoteVer, string localVer, string updateTo, string job, List<ModAction> actions, StorageMod mod)
        {
            if (remoteVer != localVer) return;
            if (updateTo != "") return;
            if (job != "") return;
            var use = actions.OfType<UseAction>().SingleOrDefault();
            Assert.NotNull(use);
            actions.Remove(use);
            Assert.Equal(use.LocalMod, mod);
        }

        private void CheckUpdate(string remoteVer, string localVer, string updateTo, string job, List<ModAction> actions, StorageMod localMod, RepoMod remoteMod)
        {
            if (localVer == "") return;
            if (job != "") return;
            if (remoteVer == localVer && updateTo == "") return;
            var update = actions.OfType<UpdateAction>().SingleOrDefault();
            Assert.NotNull(update);
            actions.Remove(update);
            Assert.Equal(update.IsContinuation, updateTo == remoteVer);
            Assert.Equal(update.Target.Hash, GetVersionHash(remoteVer));
            Assert.Equal(update.LocalMod, localMod);
            Assert.Equal(update.RemoteMod, remoteMod);
        }

        private void CheckAwaitUpdate(string remoteVer, string job, List<ModAction> actions, StorageMod localMod)
        {
            if (job == "") return;
            var update = actions.OfType<AwaitUpdateAction>().SingleOrDefault();
            Assert.NotNull(update);
            actions.Remove(update);
            Assert.Equal(update.Target.Hash, GetVersionHash(remoteVer));
            Assert.Equal(update.LocalMod, localMod);
        }

        private void SetUpdating(string version, MockSettings settings, MockStorage storage, MockStorageMod mod)
        {
            if (version == "") return;
            var target = new UpdateTarget(GetVersionHash(version), version);
            settings.Storages.Single(s => s.Name == storage.GetIdentifier()).Updating[mod.GetIdentifier()] = target;
        }

        private void SetLocal(string version, MockStorage storage, MockStorageMod mod)
        {
            if (version == "")
            {
                storage.Mods.Clear();
                return;
            }

            mod.SetFile("Version", version);
        }

        private void SetRemote(string version, MockRepo repo, MockRemoteMod mod)
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
            foreach (var remoteVer in new[] { "", "v1", "v2" })
            {
                foreach (var localVer in new[] { "", "v1", "v2", "broken" })
                {
                    foreach (var updatingTo in new[] {"", "v1", "v2"})
                    {
                        foreach (var job in new[] { "", "v1", "v2" })
                        {
                            yield return new object[] { remoteVer, localVer, updatingTo, job };
                        }
                    }
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
