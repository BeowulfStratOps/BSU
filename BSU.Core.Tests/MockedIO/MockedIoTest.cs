using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using BSU.Core.Concurrency;
using BSU.Core.Model;
using BSU.Core.Persistence;
using BSU.Core.Tests.Mocks;
using BSU.Core.Tests.Util;
using Xunit.Abstractions;

namespace BSU.Core.Tests.MockedIO;

public abstract class MockedIoTest : LoggedTest
{
    private static MockRepositoryMod CreateRepoMod(int match, int version, int ioDelayMs = 0)
    {
        var mockRepo = new MockRepositoryMod(ioDelayMs);
        for (int i = 0; i < 3; i++)
        {
            mockRepo.SetFile($"/addons/{match}_{i}.pbo", version.ToString());
        }

        return mockRepo;
    }

    private static MockStorageMod CreateStorageMod(int match, int version, int ioDelayMs = 0)
    {
        var mockStorage = new MockStorageMod(ioDelayMs);
        for (int i = 0; i < 3; i++)
        {
            mockStorage.SetFile($"/addons/{match}_{i}.pbo", version.ToString());
        }

        return mockStorage;
    }

    internal static bool FilesEqual(IModelRepositoryMod repo, IModelStorageMod storage)
    {
        var f1 = GetImplementation(repo);
        var f2 = GetImplementation(storage);
        var files1 = f1.GetFiles();
        var files2 = f2.GetFiles();
        var keys = files1.Keys.Union(files2.Keys);
        return keys.All(key => files1.ContainsKey(key) && files2.ContainsKey(key) && files1[key] == files2[key]);
    }

    internal static IModelStorage AddStorage(IModel model, string name)
    {
        return model.AddStorage("TEST", name, name);
    }

    internal static IModelRepository AddRepository(IModel model, string name)
    {
        return model.AddRepository("TEST", name, name);
    }

    private static IMockedFiles GetImplementation(object mod)
    {
        var field = mod.GetType().GetField("_implementation", BindingFlags.NonPublic | BindingFlags.Instance);
        return (IMockedFiles)field!.GetValue(mod);
    }

    private static async Task WaitFor(Func<bool> condition, int timeoutMs)
    {
        var cts = new CancellationTokenSource(timeoutMs);

        while (true)
        {
            if (condition()) return;
            if (cts.IsCancellationRequested) throw new TimeoutException();
            await Task.Yield();
        }
    }

    internal abstract class CollectionInfo : IEnumerable
    {
        public readonly string Path;
        public readonly bool Active;
        public readonly List<(string name, int match, int version)> Mods = new();

        protected CollectionInfo(string path, bool active)
        {
            Path = path;
            Active = active;
        }

        public IEnumerator GetEnumerator()
        {
            throw new NotImplementedException();
        }

        public void Add(string name, int match, int version)
        {
            Mods.Add((name, match, version));
        }
    }

    internal class RepoInfo : CollectionInfo
    {
        public RepoInfo(string path, bool active) : base(path, active)
        {
        }
    }

    internal class StorageInfo : CollectionInfo
    {
        public StorageInfo(string path, bool active) : base(path, active)
        {
        }
    }

    internal class ModelBuilder : IEnumerable
    {
        private readonly int _ioDelayMs;
        private readonly List<CollectionInfo> _infos = new();

        public ModelBuilder(int ioDelayMs = 0)
        {
            _ioDelayMs = ioDelayMs;
        }

        public Model.Model Build()
        {
            var types = new Types();

            types.AddRepoType("TEST", path =>
            {
                var repo = new MockRepository(ioDelayMs: _ioDelayMs);
                var collection = _infos.OfType<RepoInfo>().SingleOrDefault(c => c.Path == path);
                if (collection == null) return repo;
                foreach (var (name, match, version) in collection.Mods)
                {
                    repo.Mods.Add("@" + name, CreateRepoMod(match, version, _ioDelayMs));
                }

                return repo;
            });
            types.AddStorageType("TEST", path =>
            {
                var storage = new MockStorage(ioDelayMs: _ioDelayMs);
                var collection = _infos.OfType<StorageInfo>().SingleOrDefault(c => c.Path == path);
                if (collection == null) return storage;
                foreach (var (name, match, version) in collection.Mods)
                {
                    storage.Mods.Add("@" + name, CreateStorageMod(match, version, _ioDelayMs));
                }

                return storage;
            });

            var settings = new MockSettings();

            foreach (var info in _infos.OfType<RepoInfo>().Where(r => r.Active))
            {
                settings.Repositories.Add(new RepositoryEntry(info.Path, "TEST", info.Path, Guid.NewGuid()));
            }

            foreach (var info in _infos.OfType<StorageInfo>().Where(r => r.Active))
            {
                settings.Storages.Add(new StorageEntry(info.Path, "TEST", info.Path, Guid.NewGuid()));
            }

            var eventBus = new SynchronizationContextEventBus(SynchronizationContext.Current);
            var model = new Model.Model(new InternalState(settings), types, eventBus, false);

            return model;
        }

        public void Add(CollectionInfo collection)
        {
            _infos.Add(collection);
        }

        public IEnumerator GetEnumerator()
        {
            throw new NotImplementedException();
        }
    }


    protected MockedIoTest(ITestOutputHelper outputHelper) : base(outputHelper)
    {
    }
}

