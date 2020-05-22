using System.Linq;
using BSU.Core.Hashes;
using BSU.Core.JobManager;
using BSU.Core.Model;
using NLog;
using NLog.Config;
using Xunit;
using Xunit.Abstractions;

namespace BSU.Core.Tests
{
    // TODO: extend for ViewState
    public class CoreStateStories
    {
        private readonly ITestOutputHelper _outputHelper;

        public CoreStateStories(ITestOutputHelper outputHelper)
        {
            _outputHelper = outputHelper;
            var config = new LoggingConfiguration();
            var target = new XUnitTarget(outputHelper);
            config.AddRule(LogLevel.Trace, LogLevel.Fatal, target);
            LogManager.Configuration = config;
        }
        
        internal static (MockRepositoryMod, RepositoryMod) CreateRepoMod(string match, string version, IJobManager jobManager)
        {
            var mockRepo = new MockRepositoryMod();
            for (int i = 0; i < 3; i++)
            {
                mockRepo.SetFile($"/addons/{match}_{i}.pbo", version);                
            }
            var repoMod = new RepositoryMod(null, mockRepo, "myrepo", jobManager);
            return (mockRepo, repoMod);
        }
        
        internal static (MockStorageMod, StorageMod) CreateStorageMod(string match, string version, IInternalState internalState, IJobManager jobManager)
        {
            var mockStorageParent = new Model.Storage(new MockStorage(), null, null, internalState, jobManager, null);
            var mockStorage = new MockStorageMod();
            for (int i = 0; i < 3; i++)
            {
                mockStorage.SetFile($"/addons/{match}_{i}.pbo", version);                
            }
            var storageMod = new StorageMod(mockStorageParent, mockStorage, "mystorage", null, internalState, jobManager);
            return (mockStorage, storageMod);
        }

        internal static void FilesEqual(IMockedFiles f1, IMockedFiles f2)
        {
            Assert.Equal(f1.GetFiles().OrderBy(kv => kv.Key), f2.GetFiles().OrderBy(kv => kv.Key));
        }
        
        [Fact]
        private void Download()
        {
            var jobManager = new MockJobManager();
            var internalState = new MockInternalState();
            var matchMaker = new MatchMaker();
            var (repoFiles, repoMod) = CreateRepoMod("1", "1", jobManager);
            matchMaker.AddRepositoryMod(repoMod);
            jobManager.DoWork();
            var mockStorage = new MockStorage();
            var storage = new Model.Storage(mockStorage, "mystorage", "outerspcace", internalState, jobManager, matchMaker);
            jobManager.DoWork();
            var update = storage.PrepareDownload(repoMod, "mystoragemod");
            update.OnPrepared += update.Commit;
            var storageMod = storage.Mods[0];
            jobManager.DoWork();
            Assert.True(repoMod.Actions.ContainsKey(storageMod));
            Assert.Equal(ModAction.Use, repoMod.Actions[storageMod]);
            
            FilesEqual(repoFiles, storageMod.Implementation as IMockedFiles);
        }
        
        [Fact]
        private void Update()
        {
            var jobManager = new MockJobManager();
            var internalState = new MockInternalState();
            var matchMaker = new MatchMaker();
            var (repoFiles, repoMod) = CreateRepoMod("1", "1", jobManager);
            matchMaker.AddRepositoryMod(repoMod);
            jobManager.DoWork();

            var (storageFiles, storageMod) = CreateStorageMod("1", "2", internalState, jobManager);
            matchMaker.AddStorageMod(storageMod);
            jobManager.DoWork();

            Assert.Equal(ModAction.Update, repoMod.Actions[storageMod]);
            
            _outputHelper.WriteLine("Starting update...");

            var update = storageMod.PrepareUpdate(repoMod);
            update.OnPrepared += update.Commit;
            jobManager.DoWork();

            Assert.Equal(ModAction.Use, repoMod.Actions[storageMod]);
            
            FilesEqual(repoFiles, storageFiles);
        }

        [Fact]
        private void ContinueUpdate()
        {
            var jobManager = new MockJobManager();
            var internalState = new MockInternalState();
            var matchMaker = new MatchMaker();
            var (mockRepo, repoMod) = CreateRepoMod("1", "1", jobManager);
            var versionHash = new VersionHash(mockRepo).GetHashString();
            matchMaker.AddRepositoryMod(repoMod);
            jobManager.DoWork();

            
            var mockStorageParent = new Model.Storage(new MockStorage(), null, null, internalState, jobManager, matchMaker);
            var mockStorage = new MockStorageMod();
            mockStorage.SetFile("/addons/1_0.pbo", "2");
            mockStorage.SetFile("/addons/1_1.pbo", "1");
            mockStorage.SetFile("/addons/1_2.pbo", "2");
            internalState.MockUpdatingTo = new UpdateTarget(versionHash, ""); // value will be returned for any requests with unknown mods
            var storageMod = new StorageMod(mockStorageParent, mockStorage, "mystorage", null, internalState, jobManager);
            internalState.MockUpdatingTo = null;
            
            matchMaker.AddStorageMod(storageMod);
            jobManager.DoWork();

            Assert.Equal(ModAction.ContinueUpdate, repoMod.Actions[storageMod]);
            
            _outputHelper.WriteLine("Starting update...");

            var update = storageMod.PrepareUpdate(repoMod);
            update.OnPrepared += update.Commit;
            jobManager.DoWork();
            
            Assert.Equal(ModAction.Use, repoMod.Actions[storageMod]);
            Assert.Null(internalState.GetUpdateTarget(storageMod));
            
            FilesEqual(mockRepo, mockStorage);
        }

        [Fact]
        private void FixupFinishedUpdate()
        {
            // TODO:
            // Either clean it up during loading (well, hash it, it's needed for that and will take care of it. yay.)
            // Or at least make it so that empty jobs can trigger their finished thing. Not sure if that's relevant for any other situation??
            
            var jobManager = new MockJobManager();
            var internalState = new MockInternalState();
            var matchMaker = new MatchMaker();
            var (mockRepo, repoMod) = CreateRepoMod("1", "2", jobManager);
            var versionHash = new VersionHash(mockRepo).GetHashString();
            matchMaker.AddRepositoryMod(repoMod);
            jobManager.DoWork();

            internalState.MockUpdatingTo = new UpdateTarget(versionHash, ""); // value will be returned for any requests with unknown mods
            var (mockStorage, storageMod) = CreateStorageMod("1", "2", internalState, jobManager);
            internalState.MockUpdatingTo = null;
            
            matchMaker.AddStorageMod(storageMod);
            jobManager.DoWork();

            Assert.Equal(ModAction.ContinueUpdate, repoMod.Actions[storageMod]);
            
            _outputHelper.WriteLine("Starting update...");

            var update = storageMod.PrepareUpdate(repoMod);
            update.OnPrepared += update.Commit;
            jobManager.DoWork();
            
            Assert.Equal(ModAction.Use, repoMod.Actions[storageMod]);
            
            FilesEqual(mockRepo, mockStorage);
        }

        [Fact]
        private void DanglingUpdate()
        {
            // storageMod is v1/v2
            // update target is v2
            // only available repoMod is v3
            var jobManager = new MockJobManager();
            var internalState = new MockInternalState();
            var matchMaker = new MatchMaker();
            var (mockRepo, _) = CreateRepoMod("1", "2", jobManager);
            var versionHash = new VersionHash(mockRepo).GetHashString();
            var (_, repoMod) = CreateRepoMod("1", "3", jobManager);
            matchMaker.AddRepositoryMod(repoMod);
            jobManager.DoWork();

            
            var mockStorageParent = new Model.Storage(new MockStorage(), null, null, internalState, jobManager, matchMaker);
            var mockStorage = new MockStorageMod();
            mockStorage.SetFile("/addons/1_0.pbo", "2");
            mockStorage.SetFile("/addons/1_1.pbo", "1");
            mockStorage.SetFile("/addons/1_2.pbo", "2");
            internalState.MockUpdatingTo = new UpdateTarget(versionHash, ""); // value will be returned for any requests with unknown mods
            var storageMod = new StorageMod(mockStorageParent, mockStorage, "mystorage", null, internalState, jobManager);
            internalState.MockUpdatingTo = null;
            
            matchMaker.AddStorageMod(storageMod);
            jobManager.DoWork();

            Assert.False(repoMod.Actions.ContainsKey(storageMod));
        }
    }
}