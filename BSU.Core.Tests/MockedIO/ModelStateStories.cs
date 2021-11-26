using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using BSU.Core.Concurrency;
using BSU.Core.Model;
using BSU.Core.Persistence;
using BSU.Core.Services;
using BSU.Core.Tests.Mocks;
using BSU.Core.Tests.Util;
using Xunit;
using Xunit.Abstractions;

namespace BSU.Core.Tests.MockedIO
{
    // TODO: extend for ViewState
    public class ModelStateStories : LoggedTest
    {
        public ModelStateStories(ITestOutputHelper outputHelper) : base(outputHelper)
        {
        }

        private static MockRepositoryMod CreateRepoMod(int match, int version)
        {
            var mockRepo = new MockRepositoryMod();
            for (int i = 0; i < 3; i++)
            {
                mockRepo.SetFile($"/addons/{match}_{i}.pbo", version.ToString());
            }
            return mockRepo;
        }

        private static MockStorageMod CreateStorageMod(int match, int version)
        {
            throw new NotImplementedException();
        }

        private static bool FilesEqual(IModelRepositoryMod repo, IModelStorageMod storage)
        {
            var f1 = GetImplementation(repo);
            var f2 = GetImplementation(storage);
            var files1 = f1.GetFiles();
            var files2 = f2.GetFiles();
            var keys = files1.Keys.Union(files2.Keys);
            return keys.All(key => files1.ContainsKey(key) && files2.ContainsKey(key) && files1[key] == files2[key]);
        }

        private static (TestEventBus eventBus, Model.Model model) GetModel(CollectionInfo[] repositories,
            CollectionInfo[] storages)
        {
            var types = new Types();

            types.AddRepoType("TEST", path =>
            {
                var repo = new MockRepository();
                var collection = repositories.SingleOrDefault(c => c.Path == path);
                if (collection == null) return repo;
                foreach (var (name, match, version) in collection.Mods)
                {
                    repo.Mods.Add("@" + name, CreateRepoMod(match, version));
                }
                return repo;
            });
            types.AddStorageType("TEST", path =>
            {
                var storage = new MockStorage();
                var collection = storages.SingleOrDefault(c => c.Path == path);
                if (collection == null) return storage;
                foreach (var (name, match, version) in collection.Mods)
                {
                    storage.Mods.Add("@" + name, CreateStorageMod(match, version));
                }
                return storage;
            });

            var settings = new MockSettings();

            foreach (var (path, _, _) in repositories.Where(r => r.Active))
            {
                settings.Repositories.Add(new RepositoryEntry(path, "TEST", path, Guid.NewGuid()));
            }
            foreach (var (path, _, _) in storages.Where(r => r.Active))
            {
                settings.Storages.Add(new StorageEntry(path, "TEST", path, Guid.NewGuid()));
            }

            var eventBus = new TestEventBus();
            var model = new Model.Model(new InternalState(settings), types, eventBus, false);

            return (eventBus, model);
        }

        private record CollectionInfo(string Path, bool Active, params (string name, int match, int version)[] Mods);

        private static IModelStorage AddStorage(IModel model, string name)
        {
            return model.AddStorage("TEST", name, name);
        }

        private static IModelRepository AddRepository(IModel model, string name)
        {
            return model.AddRepository("TEST", name, name);
        }

        private static IMockedFiles GetImplementation(object mod)
        {
            var field = mod.GetType().GetField("_implementation", BindingFlags.NonPublic | BindingFlags.Instance);
            return (IMockedFiles)field!.GetValue(mod);
        }

        [Fact]
        private async Task Download()
        {
            var (eventBus, model) = GetModel(new[]
            {
                new CollectionInfo("repo", false, ("mod", 1, 1))
            }, new []
            {
                new CollectionInfo("storage", false)
            });

            var storage = AddStorage(model, "storage");
            var repo = AddRepository(model, "repo");

            eventBus.Work(100);

            var repoMod = repo.GetMods()[0];
            repoMod.SetSelection(new RepositoryModActionDownload(storage));

            var update = await repoMod.StartUpdate(null, CancellationToken.None);
            await update.Update;

            var storageMod = storage.GetMods().Single();

            eventBus.Work(50);

            Assert.Equal(ModActionEnum.Use, CoreCalculation.GetModAction(repoMod, storageMod));

            Assert.True(FilesEqual(repoMod, storage.GetMods()[0]));
        }
/*
        [Fact]
        private void Update()
        {
            var structure = new MockModelStructure();
            var matchMaker = new MatchMaker(structure);
            var worker = new MockWorker();

            var (repoFiles, repoMod) = CreateRepoMod("1", "1", worker, structure);
            matchMaker.AddRepositoryMod(repoMod);

            var (storageFiles, storageMod) = CreateStorageMod("1", "2", worker, structure);
            matchMaker.AddStorageMod(storageMod);
            worker.DoWork();

            Assert.Equal(ModActionEnum.Update, repoMod.LocalMods[storageMod].ActionType);

            OutputHelper.WriteLine("Starting update...");

            var update = storageMod.PrepareUpdate(repoMod.Implementation, repoMod.AsUpdateTarget);
            update.OnStateChange += () =>
            {
                if (update.State != UpdateState.Prepared) return;
                Assert.True(update.GetPrepStats() > 0);
                update.Continue();
            };
            update.Continue();
            worker.DoWork();

            Assert.Equal(ModActionEnum.Use, repoMod.LocalMods[storageMod].ActionType);

            Assert.True(FilesEqual(repoFiles, storageFiles));
        }

        [Fact]
        private void ContinueUpdate()
        {
            var structure = new MockModelStructure();
            var matchMaker = new MatchMaker(structure);
            var worker = new MockWorker();

            var (mockRepo, repoMod) = CreateRepoMod("1", "1", worker, structure);
            var versionHash = new VersionHash(mockRepo).GetHashString();
            matchMaker.AddRepositoryMod(repoMod);


            var mockStorage = new MockStorageMod();
            mockStorage.SetFile("/addons/1_0.pbo", "2");
            mockStorage.SetFile("/addons/1_1.pbo", "1");
            mockStorage.SetFile("/addons/1_2.pbo", "2");
            var state = new MockPersistedStorageModState {UpdateTarget = new UpdateTarget(versionHash, "")};
            var storageMod = new StorageMod(worker, mockStorage, "mystorage", null, state, worker, Guid.Empty, true);
            structure.StorageMods.Add(storageMod);
            matchMaker.AddStorageMod(storageMod);
            worker.DoWork();

            Assert.Equal(ModActionEnum.ContinueUpdate, repoMod.LocalMods[storageMod].ActionType);

            OutputHelper.WriteLine("Starting update...");

            var update = storageMod.PrepareUpdate(repoMod.Implementation, repoMod.AsUpdateTarget);
            update.OnStateChange += () =>
            {
                if (update.State != UpdateState.Prepared) return;
                Assert.True(update.GetPrepStats() > 0);
                update.Continue();
            };
            update.Continue();
            worker.DoWork();

            Assert.Equal(ModActionEnum.Use, repoMod.LocalMods[storageMod].ActionType);
            Assert.Null(state.UpdateTarget);

            Assert.True(FilesEqual(mockRepo, mockStorage));
        }

        [Fact]
        private void FixupFinishedUpdate()
        {
            // TODO:
            // Either clean it up during loading (well, hash it, it's needed for that and will take care of it. yay.)
            // Or at least make it so that empty jobs can trigger their finished thing. Not sure if that's relevant for any other situation??

            var structure = new MockModelStructure();
            var matchMaker = new MatchMaker(structure);
            var worker = new MockWorker();

            var (mockRepo, repoMod) = CreateRepoMod("1", "2", worker, structure);
            var versionHash = new VersionHash(mockRepo).GetHashString();
            matchMaker.AddRepositoryMod(repoMod);

            var (mockStorage, storageMod) = CreateStorageMod("1", "2", worker, structure, new UpdateTarget(versionHash, ""));

            matchMaker.AddStorageMod(storageMod);
            worker.DoWork();

            Assert.Equal(ModActionEnum.ContinueUpdate, repoMod.LocalMods[storageMod].ActionType);

            OutputHelper.WriteLine("Starting update...");

            var update = storageMod.PrepareUpdate(repoMod.Implementation, repoMod.AsUpdateTarget);
            update.OnStateChange += () =>
            {
                if (update.State != UpdateState.Prepared) return;
                Assert.Equal(0, update.GetPrepStats());
                update.Continue();
            };
            update.Continue();
            worker.DoWork();

            Assert.Equal(ModActionEnum.Use, repoMod.LocalMods[storageMod].ActionType);

            Assert.True(FilesEqual(mockRepo, mockStorage));
        }

        [Fact]
        private void DanglingUpdate()
        {
            // storageMod is v1/v2
            // update target is v2
            // only available repoMod is v3
            var structure = new MockModelStructure();
            var matchMaker = new MatchMaker(structure);
            var worker = new MockWorker();

            var (mockRepo, _) = CreateRepoMod("1", "2", worker, structure);
            var versionHash = new VersionHash(mockRepo).GetHashString();
            var (_, repoMod) = CreateRepoMod("1", "3", worker, structure);
            matchMaker.AddRepositoryMod(repoMod);


            var mockStorage = new MockStorageMod();
            mockStorage.SetFile("/addons/1_0.pbo", "2");
            mockStorage.SetFile("/addons/1_1.pbo", "1");
            mockStorage.SetFile("/addons/1_2.pbo", "2");
            var state = new MockPersistedStorageModState {UpdateTarget = new UpdateTarget(versionHash, "")};
            var storageMod = new StorageMod(worker, mockStorage, "mystorage", null, state, worker, Guid.Empty, true);
            structure.StorageMods.Add(storageMod);
            matchMaker.AddStorageMod(storageMod);
            worker.DoWork();

            Assert.False(repoMod.LocalMods.ContainsKey(storageMod));

            storageMod.Abort();
            worker.DoWork();

            Assert.True(repoMod.LocalMods.ContainsKey(storageMod));
            Assert.Equal(ModActionEnum.Update, repoMod.LocalMods[storageMod].ActionType);
        }

        [Fact]
        private void AbortDownload()
        {
            var structure = new MockModelStructure();
            var matchMaker = new MatchMaker(structure);
            var worker = new MockWorker();

            var (_, repoMod) = CreateRepoMod("1", "1", worker, structure);
            matchMaker.AddRepositoryMod(repoMod);
            worker.DoWork();
            var mockStorage = new MockStorage();
            var storage = new Model.Storage(mockStorage, "mystorage", "outerspcace", new MockStorageState(), worker, matchMaker, worker);
            worker.DoWork();
            var x = new List<MockStorageMod>();
            var update = storage.PrepareDownload(repoMod.Implementation, repoMod.AsUpdateTarget, "mystoragemod");

            update.OnStateChange += () =>
            {
                if (update.State == UpdateState.Created)
                {
                    x.Add(mockStorage.Mods.Values.First());
                    update.Continue();
                    return;
                }
                if (update.State != UpdateState.Prepared) return;
                Assert.True(update.GetPrepStats() > 0);
                update.Abort();
            };

            update.Continue();
            worker.DoWork();

            Assert.Empty(repoMod.LocalMods);
            Assert.Empty(x[0].GetFiles());
            Assert.Empty(mockStorage.Mods);
        }

        [Fact]
        private void AbortUpdate()
        {
            var structure = new MockModelStructure();
            var matchMaker = new MatchMaker(structure);
            var worker = new MockWorker();

            var (_, repoMod) = CreateRepoMod("1", "1", worker, structure);
            matchMaker.AddRepositoryMod(repoMod);
            worker.DoWork();

            var (referenceFiles, _) = CreateStorageMod("1", "2", worker, structure);
            var (storageFiles, storageMod) = CreateStorageMod("1", "2", worker, structure);
            matchMaker.AddStorageMod(storageMod);
            worker.DoWork();

            Assert.Equal(ModActionEnum.Update, repoMod.LocalMods[storageMod].ActionType);

            OutputHelper.WriteLine("Starting update...");

            var update = storageMod.PrepareUpdate(repoMod.Implementation, repoMod.AsUpdateTarget);
            update.OnStateChange += () =>
            {
                if (update.State != UpdateState.Prepared) return;
                update.Abort();
            };
            update.Continue();
            worker.DoWork();

            Assert.Equal(ModActionEnum.Update, repoMod.LocalMods[storageMod].ActionType);

            Assert.True(FilesEqual(referenceFiles, storageFiles));
        }

        [Fact]
        private void AbortUpdateJob()
        {
            var structure = new MockModelStructure();
            var matchMaker = new MatchMaker(structure);
            var worker = new MockWorker();

            var (_, repoMod) = CreateRepoMod("1", "1", worker, structure);
            matchMaker.AddRepositoryMod(repoMod);
            worker.DoWork();

            var (referenceFiles, _) = CreateStorageMod("1", "2", worker, structure);
            var (storageFiles, storageMod) = CreateStorageMod("1", "2", worker, structure);
            matchMaker.AddStorageMod(storageMod);
            worker.DoWork();

            Assert.Equal(ModActionEnum.Update, repoMod.LocalMods[storageMod].ActionType);

            OutputHelper.WriteLine("Starting update...");

            var update = storageMod.PrepareUpdate(repoMod.Implementation, repoMod.AsUpdateTarget);
            update.Continue();
            worker.DoWork();
            Assert.True(update.State == UpdateState.Prepared);
            update.Continue();
            worker.DoJobStep();
            worker.GetActiveJobs().First().Abort();
            worker.DoWork();

            Assert.Equal(ModActionEnum.Update, repoMod.LocalMods[storageMod].ActionType);

            Assert.False(FilesEqual(referenceFiles, storageFiles));
        }

        [Fact]
        private void ErrorPrepare()
        {
            var structure = new MockModelStructure();
            var matchMaker = new MatchMaker(structure);
            var worker = new MockWorker();

            var (_, repoMod) = CreateRepoMod("1", "1", worker, structure);
            matchMaker.AddRepositoryMod(repoMod);
            worker.DoWork();

            CreateStorageMod("1", "2", worker, structure);
            var (storageFiles, storageMod) = CreateStorageMod("1", "2", worker, structure);
            matchMaker.AddStorageMod(storageMod);
            worker.DoWork();

            Assert.Equal(ModActionEnum.Update, repoMod.LocalMods[storageMod].ActionType);

            OutputHelper.WriteLine("Starting update...");

            var prepared = false;
            var update = storageMod.PrepareUpdate(repoMod.Implementation, repoMod.AsUpdateTarget);
            update.OnStateChange += () =>
            {
                if (update.State == UpdateState.Prepared) prepared = true;
            };
            update.Continue();
            storageFiles.ThrowErrorOpen = true;
            worker.DoQueueStep(); // might ot be needed anymore due to Prepare()?
            worker.DoJobStep();
            storageFiles.ThrowErrorOpen = false;
            worker.DoWork();

            Assert.NotEqual(UpdateState.Prepared, update.State);
            Assert.False(prepared);

            // TODO: make sure events fire
            Assert.True(repoMod.LocalMods.ContainsKey(storageMod));
            Assert.Equal(ModActionEnum.Error, repoMod.LocalMods[storageMod].ActionType);
            Assert.Equal(StorageModStateEnum.ErrorUpdate, storageMod.GetState().State);
        }

        [Fact]
        private void ErrorUpdate()
        {
            var structure = new MockModelStructure();
            var matchMaker = new MatchMaker(structure);
            var worker = new MockWorker();

            var (_, repoMod) = CreateRepoMod("1", "1", worker, structure);
            matchMaker.AddRepositoryMod(repoMod);
            worker.DoWork();

            CreateStorageMod("1", "2", worker, structure);
            var (storageFiles, storageMod) = CreateStorageMod("1", "2", worker, structure);
            matchMaker.AddStorageMod(storageMod);
            worker.DoWork();

            Assert.Equal(ModActionEnum.Update, repoMod.LocalMods[storageMod].ActionType);

            OutputHelper.WriteLine("Starting update...");

            var update = storageMod.PrepareUpdate(repoMod.Implementation, repoMod.AsUpdateTarget);
            update.Continue();
            worker.DoWork();
            Assert.Equal(UpdateState.Prepared, update.State);
            update.Continue();
            worker.DoJobStep();
            storageFiles.ThrowErrorOpen = true;
            worker.DoJobStep();
            storageFiles.ThrowErrorOpen = false;
            worker.DoWork();

            // TODO: make sure events fire
            Assert.True(repoMod.LocalMods.ContainsKey(storageMod));
            Assert.Equal(ModActionEnum.Error, repoMod.LocalMods[storageMod].ActionType);
            Assert.Equal(StorageModStateEnum.ErrorUpdate, storageMod.GetState().State);
        }

        [Fact]
        private void ErrorLoad()
        {
            var structure = new MockModelStructure();
            var matchMaker = new MatchMaker(structure);
            var worker = new MockWorker();

            var (_, repoMod) = CreateRepoMod("1", "1", worker, structure);
            matchMaker.AddRepositoryMod(repoMod);
            worker.DoWork();

            var (storageFiles, storageMod) = CreateStorageMod("1", "2", worker, structure);
            storageFiles.ThrowErrorLoad = true;
            matchMaker.AddStorageMod(storageMod);
            worker.DoWork();

            // TODO: make sure events fire
            Assert.False(repoMod.LocalMods.ContainsKey(storageMod));
            Assert.Equal(StorageModStateEnum.ErrorLoad, storageMod.GetState().State);
        }

        [Fact]
        private void ErrorHash()
        {
            var structure = new MockModelStructure();
            var matchMaker = new MatchMaker(structure);
            var worker = new MockWorker();

            var (_, repoMod) = CreateRepoMod("1", "1", worker, structure);
            matchMaker.AddRepositoryMod(repoMod);
            worker.DoWork();

            var (storageFiles, storageMod) = CreateStorageMod("1", "2", worker, structure);
            storageFiles.ThrowErrorOpen = true;
            matchMaker.AddStorageMod(storageMod);
            worker.DoWork();

            // TODO: make sure events fire
            Assert.False(repoMod.LocalMods.ContainsKey(storageMod));
            Assert.Equal(StorageModStateEnum.ErrorLoad, storageMod.GetState().State);
        }

        [Fact]
        private void ErrorLoadRepo()
        {
            var structure = new MockModelStructure();
            var matchMaker = new MatchMaker(structure);
            var worker = new MockWorker();

            var (repoFiles, repoMod) = CreateRepoMod("1", "1", worker, structure);
            repoFiles.ThrowErrorLoad = true;
            matchMaker.AddRepositoryMod(repoMod);
            worker.DoWork();

            var (_, storageMod) = CreateStorageMod("1", "2", worker, structure);
            matchMaker.AddStorageMod(storageMod);
            worker.DoWork();

            // TODO: make sure events fire
            Assert.False(repoMod.LocalMods.ContainsKey(storageMod));
            Assert.NotNull(repoMod.GetState().Error);
        }*/
    }
}
