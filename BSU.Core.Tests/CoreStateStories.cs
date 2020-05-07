using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using BSU.Core.Hashes;
using BSU.Core.JobManager;
using BSU.Core.Model;
using BSU.CoreCommon;
using NLog;
using NLog.Config;
using NLog.Targets;
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

        private (MockRepositoryMod, RepositoryMod) CreateRepoMod(string match, string version, IJobManager jobManager)
        {
            var mockRepo = new MockRepositoryMod();
            for (int i = 0; i < 3; i++)
            {
                mockRepo.SetFile($"/addons/{match}_{i}.pbo", version);                
            }
            var repoMod = new RepositoryMod(null, mockRepo, "myrepo", jobManager);
            return (mockRepo, repoMod);
        }
        
        private (MockStorageMod, StorageMod) CreateStorageMod(string match, string version, IInternalState internalState, IJobManager jobManager)
        {
            var mockStorageParent = new Model.Storage(new MockStorage(), null, null, internalState, jobManager);
            var mockStorage = new MockStorageMod();
            for (int i = 0; i < 3; i++)
            {
                mockStorage.SetFile($"/addons/{match}_{i}.pbo", version);                
            }
            var storageMod = new StorageMod(mockStorageParent, mockStorage, "mystorage", null, internalState, jobManager);
            return (mockStorage, storageMod);
        }
        
        [Fact]
        private void Download()
        {
            var jobManager = new MockJobManager();
            var internalState = new MockInternalState();
            var matchMaker = new MatchMaker();
            var (_, repoMod) = CreateRepoMod("1", "1", jobManager);
            matchMaker.AddRepositoryMod(repoMod);
            jobManager.DoWork();
            var mockStorage = new MockStorage();
            var storage = new Model.Storage(mockStorage, "mystorage", "outerspcace", internalState, jobManager);
            jobManager.DoWork();
            storage.StartDownload(repoMod, "mystoragemod", matchMaker);
            var storageMod = storage.Mods[0];
            jobManager.DoWork();
            Assert.True(repoMod.Actions.ContainsKey(storageMod));
            Assert.Equal(ModAction.Use, repoMod.Actions[storageMod]);
        }
        
        [Fact]
        private void Update()
        {
            var jobManager = new MockJobManager();
            var internalState = new MockInternalState();
            var matchMaker = new MatchMaker();
            var (_, repoMod) = CreateRepoMod("1", "1", jobManager);
            matchMaker.AddRepositoryMod(repoMod);
            jobManager.DoWork();

            var (_, storageMod) = CreateStorageMod("1", "2", internalState, jobManager);
            matchMaker.AddStorageMod(storageMod);
            jobManager.DoWork();

            Assert.Equal(ModAction.Update, repoMod.Actions[storageMod]);
            
            _outputHelper.WriteLine("Starting update...");

            storageMod.StartUpdate(repoMod);
            jobManager.DoWork();
            
            Assert.Equal(ModAction.Use, repoMod.Actions[storageMod]);
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

            
            var mockStorageParent = new Model.Storage(new MockStorage(), null, null, internalState, jobManager);
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

            storageMod.StartUpdate(repoMod);
            jobManager.DoWork();
            
            Assert.Equal(ModAction.Use, repoMod.Actions[storageMod]);
        }

        [Fact]
        private void FixupFinishedUpdate()
        {
            // TODO:
            // Either clean it up during loading (well, hash it, it's needed for that and will take care of it. yay.)
            // Or at least make it so that empty jobs can trigger their finished thing. Not sure if that's relevant for any other situation??
        }
        
        // TODO: acre2 exe file keeps being locked when trying to hash after update (I think). not sure if AV or us.
    }
}