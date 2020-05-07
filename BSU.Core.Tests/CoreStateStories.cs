using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using BSU.Core.Hashes;
using BSU.Core.JobManager;
using BSU.Core.Model;
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
        public CoreStateStories(ITestOutputHelper outputHelper)
        {
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
            var mockRepo = new MockRepositoryMod(); // TODO: pass match and version in constructor and prefill
            var repoMod = new RepositoryMod(null, mockRepo, "myrepo", jobManager);
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
        }
    }
}