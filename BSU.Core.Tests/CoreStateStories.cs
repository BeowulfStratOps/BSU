using System;
using System.Collections.Generic;
using System.Linq;
using BSU.Core.Hashes;
using BSU.Core.Model;
using BSU.Core.Tests.Mocks;
using Xunit;
using Xunit.Abstractions;

namespace BSU.Core.Tests
{
    // TODO: extend for ViewState
    public class CoreStateStories : LoggedTest
    {
        public CoreStateStories(ITestOutputHelper outputHelper) : base(outputHelper)
        {
        }

        internal static (MockRepositoryMod, RepositoryMod) CreateRepoMod(string match, string version, MockWorker worker, MockModelStructure structure)
        {
            var mockRepo = new MockRepositoryMod();
            for (int i = 0; i < 3; i++)
            {
                mockRepo.SetFile($"/addons/{match}_{i}.pbo", version);
            }
            var repoMod = new RepositoryMod(worker, mockRepo, "myrepo", worker, new MockRepositoryModState(), new RelatedActionsBag(), structure);
            structure.RepositoryMods.Add(repoMod);
            return (mockRepo, repoMod);
        }

        internal static (MockStorageMod, StorageMod) CreateStorageMod(string match, string version, MockWorker worker, MockModelStructure structure, UpdateTarget stateTarget = null)
        {
            var mockStorage = new MockStorageMod();
            for (int i = 0; i < 3; i++)
            {
                mockStorage.SetFile($"/addons/{match}_{i}.pbo", version);
            }

            var state = new MockStorageModState {UpdateTarget = stateTarget};
            var storageMod = new StorageMod(worker, mockStorage, "mystorage", null, state, worker, Guid.Empty, true);
            structure.StorageMods.Add(storageMod);
            return (mockStorage, storageMod);
        }

        internal static bool FilesEqual(IMockedFiles f1, IMockedFiles f2)
        {
            var files1 = f1.GetFiles();
            var files2 = f2.GetFiles();
            var keys = new HashSet<string>(files1.Keys);
            foreach (var key in files2.Keys)
            {
                keys.Add(key);
            }

            return keys.All(key => files1.ContainsKey(key) && files2.ContainsKey(key) && files1[key] == files2[key]);
        }

        [Fact]
        private void Download()
        {
            var structure = new MockModelStructure();
            var matchMaker = new MatchMaker(structure);
            var worker = new MockWorker();
            var (repoFiles, repoMod) = CreateRepoMod("1", "1", worker, structure);
            matchMaker.AddRepositoryMod(repoMod);

            var mockStorage = new MockStorage();
            var storageState = new MockStorageState();
            var storage = new Model.Storage(mockStorage, "mystorage", "outerspcace", storageState, worker, matchMaker, worker);
            worker.DoWork();
            var update = storage.PrepareDownload(repoMod.Implementation, repoMod.AsUpdateTarget, "mystoragemod");

            update.OnStateChange += () =>
            {
                if (update.State == UpdateState.Created)
                {
                    update.Continue();
                    return;
                }
                if (update.State != UpdateState.Prepared) return;
                Assert.True(update.GetPrepStats() > 0);
                update.Continue();
            };
            update.Continue();
        
            worker.DoWork();
            var storageMod = storage.Mods[0];
            Assert.True(repoMod.Actions.ContainsKey(storageMod));
            Assert.Equal(ModActionEnum.Use, repoMod.Actions[storageMod].ActionType);

            Assert.True(FilesEqual(repoFiles, mockStorage.Mods.Values.First()));
        }

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

            Assert.Equal(ModActionEnum.Update, repoMod.Actions[storageMod].ActionType);

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

            Assert.Equal(ModActionEnum.Use, repoMod.Actions[storageMod].ActionType);

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
            var state = new MockStorageModState {UpdateTarget = new UpdateTarget(versionHash, "")};
            var storageMod = new StorageMod(worker, mockStorage, "mystorage", null, state, worker, Guid.Empty, true);
            structure.StorageMods.Add(storageMod);
            matchMaker.AddStorageMod(storageMod);
            worker.DoWork();

            Assert.Equal(ModActionEnum.ContinueUpdate, repoMod.Actions[storageMod].ActionType);

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

            Assert.Equal(ModActionEnum.Use, repoMod.Actions[storageMod].ActionType);
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

            Assert.Equal(ModActionEnum.ContinueUpdate, repoMod.Actions[storageMod].ActionType);

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

            Assert.Equal(ModActionEnum.Use, repoMod.Actions[storageMod].ActionType);

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
            var state = new MockStorageModState {UpdateTarget = new UpdateTarget(versionHash, "")};
            var storageMod = new StorageMod(worker, mockStorage, "mystorage", null, state, worker, Guid.Empty, true);
            structure.StorageMods.Add(storageMod);
            matchMaker.AddStorageMod(storageMod);
            worker.DoWork();

            Assert.False(repoMod.Actions.ContainsKey(storageMod));

            storageMod.Abort();
            worker.DoWork();

            Assert.True(repoMod.Actions.ContainsKey(storageMod));
            Assert.Equal(ModActionEnum.Update, repoMod.Actions[storageMod].ActionType);
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

            Assert.Empty(repoMod.Actions);
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

            Assert.Equal(ModActionEnum.Update, repoMod.Actions[storageMod].ActionType);

            OutputHelper.WriteLine("Starting update...");

            var update = storageMod.PrepareUpdate(repoMod.Implementation, repoMod.AsUpdateTarget);
            update.OnStateChange += () =>
            {
                if (update.State != UpdateState.Prepared) return;
                update.Abort();
            };
            update.Continue();
            worker.DoWork();

            Assert.Equal(ModActionEnum.Update, repoMod.Actions[storageMod].ActionType);

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

            Assert.Equal(ModActionEnum.Update, repoMod.Actions[storageMod].ActionType);

            OutputHelper.WriteLine("Starting update...");

            var update = storageMod.PrepareUpdate(repoMod.Implementation, repoMod.AsUpdateTarget);
            update.Continue();
            worker.DoWork();
            Assert.True(update.State == UpdateState.Prepared);
            update.Continue();
            worker.DoJobStep();
            worker.GetActiveJobs().First().Abort();
            worker.DoWork();

            Assert.Equal(ModActionEnum.Update, repoMod.Actions[storageMod].ActionType);

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

            Assert.Equal(ModActionEnum.Update, repoMod.Actions[storageMod].ActionType);

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
            Assert.True(repoMod.Actions.ContainsKey(storageMod));
            Assert.Equal(ModActionEnum.Error, repoMod.Actions[storageMod].ActionType);
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

            Assert.Equal(ModActionEnum.Update, repoMod.Actions[storageMod].ActionType);

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
            Assert.True(repoMod.Actions.ContainsKey(storageMod));
            Assert.Equal(ModActionEnum.Error, repoMod.Actions[storageMod].ActionType);
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
            Assert.False(repoMod.Actions.ContainsKey(storageMod));
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
            Assert.False(repoMod.Actions.ContainsKey(storageMod));
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
            Assert.False(repoMod.Actions.ContainsKey(storageMod));
            Assert.NotNull(repoMod.GetState().Error);
        }
    }
}
