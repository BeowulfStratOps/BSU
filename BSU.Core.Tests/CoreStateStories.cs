using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BSU.Core.State;
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
            return MockRepo.Repos[name];
        }

        private MockStorage AddStorage(Core core, string name)
        {
            core.AddStorage(name, new DirectoryInfo("path/" + name), "MOCK");
            return MockStorage.Storages[name];
        }

        private (Core, MockSettings, MockRepo, MockRemoteMod, MockStorageMod, MockStorage) DoSetup()
        {
            var settings = new MockSettings();
            var core = new Core(settings);
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
        private void Check(string remoteVer, string localVer, string updatingTo)
        {
            var (core, settings, repo, remoteMod, localMod, storage) = DoSetup();
            SetRemote(remoteVer, repo, remoteMod);
            SetLocal(localVer, storage, localMod);
            SetUpdating(updatingTo, settings, storage, localMod);
            var state = core.GetState();
            var actions = state.Repos.First().Mods.SelectMany(m => m.Actions).ToList();
            if (remoteVer != "")
            {
                CheckDownload(remoteVer, actions, state.Storages.Single());
                CheckUse(remoteVer, localVer, updatingTo, actions, state.Storages.Single().Mods.SingleOrDefault());
                CheckUpdate(remoteVer, localVer, updatingTo, actions, state.Storages.Single().Mods.SingleOrDefault(),
                    state.Repos.Single().Mods.Single());
                CheckAwaitUpdate(remoteVer, localVer, updatingTo, actions, state.Storages.Single().Mods.SingleOrDefault());
            }
            Assert.Empty(actions);
        }

        private void CheckDownload(string remoteVer, List<ModAction> actions, Storage storage)
        {
            var download = actions.OfType<DownloadAction>().SingleOrDefault();
            Assert.NotNull(download);
            actions.Remove(download);
            Assert.Equal(download.Storage, storage);
        }

        private void CheckUse(string remoteVer, string localVer, string updateTo, List<ModAction> actions, StorageMod mod)
        {
            if (remoteVer != localVer) return;
            if (updateTo != "" && updateTo != remoteVer) return;
            var use = actions.OfType<UseAction>().SingleOrDefault();
            Assert.NotNull(use);
            actions.Remove(use);
            Assert.Equal(use.LocalMod, mod);
        }

        private void CheckUpdate(string remoteVer, string localVer, string updateTo, List<ModAction> actions, StorageMod localMod, RepoMod remoteMod)
        {
            if (remoteVer == localVer && (remoteVer == updateTo || updateTo == "") || localVer == "") return;
            if (remoteVer == updateTo) return;
            var update = actions.OfType<UpdateAction>().SingleOrDefault();
            Assert.NotNull(update);
            actions.Remove(update);
            Assert.Equal(update.Target.Hash, GetVersionHash(remoteVer));
            Assert.Equal(update.LocalMod, localMod);
            Assert.Equal(update.RemoteMod, remoteMod);
        }

        private void CheckAwaitUpdate(string remoteVer, string localVer, string updateTo, List<ModAction> actions, StorageMod localMod)
        {
            if (remoteVer == localVer || localVer == "") return;
            if (remoteVer != updateTo) return;
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
                        yield return new object[] {remoteVer, localVer, updatingTo};
                    }
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
