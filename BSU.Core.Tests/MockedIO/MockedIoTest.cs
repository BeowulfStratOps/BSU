using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using BSU.Core.Concurrency;
using BSU.Core.Ioc;
using BSU.Core.Model;
using BSU.Core.Persistence;
using BSU.Core.Tests.Mocks;
using BSU.Core.Tests.Util;
using BSU.Core.ViewModel;
using Xunit.Abstractions;

namespace BSU.Core.Tests.MockedIO;

public abstract class MockedIoTest : LoggedTest
{
    private static MockRepositoryMod CreateRepoMod(int match, int version, Task? load)
    {
        var mockRepo = new MockRepositoryMod(load);
        for (int i = 0; i < 3; i++)
        {
            mockRepo.SetFile($"/addons/{match}_{i}.pbo", version.ToString());
        }

        return mockRepo;
    }

    private static MockStorageMod CreateStorageMod(int match, int version, Task? load)
    {
        var mockStorage = new MockStorageMod(load);
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
        return model.AddRepository("TEST", name, name, null!);
    }

    private static IMockedFiles GetImplementation(object mod)
    {
        var field = mod.GetType().GetField("_implementation", BindingFlags.NonPublic | BindingFlags.Instance);
        return (IMockedFiles)field!.GetValue(mod)!;
    }

    protected static async Task WaitFor(int timeoutMs, Func<bool> condition)
    {
        var cts = new CancellationTokenSource(timeoutMs);

        while (true)
        {
            if (condition()) return;
            if (cts.IsCancellationRequested) throw new TimeoutException();
            await Task.Delay(1, CancellationToken.None);
        }
    }

    internal abstract class CollectionInfo : IEnumerable
    {
        public readonly Task? Load;
        public readonly string Path;
        public readonly bool Active;
        public readonly List<(string name, int match, int version, Task? load)> Mods = new();

        protected CollectionInfo(string path, bool active, Task? load)
        {
            Load = load;
            Path = path;
            Active = active;
        }

        public IEnumerator GetEnumerator()
        {
            throw new NotImplementedException();
        }

        public void Add(string name, int match, int version)
        {
            Mods.Add((name, match, version, Task.CompletedTask));
        }

        public void Add(string name, int match, int version, Task load)
        {
            Mods.Add((name, match, version, load));
        }
    }

    internal class RepoInfo : CollectionInfo
    {
        public RepoInfo(string path, bool active = true, Task? load = null) : base(path, active, load)
        {
        }
    }

    internal class StorageInfo : CollectionInfo
    {
        public StorageInfo(string path, bool active = true, Task? load = null) : base(path, active, load)
        {
        }
    }

    internal class ModelBuilder : IEnumerable
    {
        private readonly List<CollectionInfo> _infos = new();

        public Model.Model Build(ServiceProvider? serviceProvider = null)
        {
            var types = new Types();

            types.AddRepoType("TEST", path =>
            {
                var collection = _infos.OfType<RepoInfo>().SingleOrDefault(c => c.Path == path);
                if (collection == null) return new MockRepository(Task.CompletedTask);
                var repo = new MockRepository(collection.Load);
                foreach (var (name, match, version, load) in collection.Mods)
                {
                    repo.Mods.Add("@" + name, CreateRepoMod(match, version, load));
                }

                return repo;
            });
            types.AddStorageType("TEST", path =>
            {
                var collection = _infos.OfType<StorageInfo>().SingleOrDefault(c => c.Path == path);
                if (collection == null) return new MockStorage(Task.CompletedTask);
                var storage = new MockStorage(collection.Load);
                foreach (var (name, match, version, load) in collection.Mods)
                {
                    storage.Mods.Add("@" + name, CreateStorageMod(match, version, load));
                }

                return storage;
            });

            var settings = new MockSettings();

            foreach (var info in _infos.OfType<RepoInfo>().Where(r => r.Active))
            {
                settings.Repositories.Add(new RepositoryEntry(info.Path, "TEST", info.Path, Guid.NewGuid(), null!));
            }

            foreach (var info in _infos.OfType<StorageInfo>().Where(r => r.Active))
            {
                settings.Storages.Add(new StorageEntry(info.Path, "TEST", info.Path, Guid.NewGuid()));
            }

            var services = serviceProvider ?? new ServiceProvider();
            var dispatcher = new SynchronizationContextDispatcher(SynchronizationContext.Current ?? throw new InvalidOperationException());
            services.Add<IDispatcher>(dispatcher);
            services.Add(types);
            var model = new Model.Model(new InternalState(settings), services, false);
            model.Load();
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

        public ViewModel.ViewModel BuildVm(IInteractionService? interactionService = null)
        {
            var serviceProvider = new ServiceProvider();
            if (interactionService != null)
                serviceProvider.Add(interactionService);
            Build();
            return new ViewModel.ViewModel(serviceProvider);
        }
    }


    protected MockedIoTest(ITestOutputHelper outputHelper) : base(outputHelper)
    {
        Thread.CurrentThread.Name = "Main"; // looks better in logs
    }
}

