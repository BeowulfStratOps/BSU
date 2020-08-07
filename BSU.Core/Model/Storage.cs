using System;
using System.Collections.Generic;
using System.Threading;
using BSU.Core.JobManager;
using BSU.Core.Persistence;
using BSU.CoreCommon;

namespace BSU.Core.Model
{
    internal class Storage : IModelStorage
    {
        private readonly IStorageState _internalState;
        private readonly IJobManager _jobManager;
        private readonly IMatchMaker _matchMaker;
        public IStorage Implementation { get; }
        public string Identifier { get; }
        public string Location { get; }
        public Uid Uid { get; } = new Uid();
        public List<IModelStorageMod> Mods { private set; get; } = new List<IModelStorageMod>(); // TODO: readonly

        public JobSlot<SimpleJob> Loading { get; }

        private readonly IActionQueue _actionQueue;

        public Storage(IStorage implementation, string identifier, string location, IStorageState internalState, IJobManager jobManager, IMatchMaker matchMaker, IActionQueue actionQueue)
        {
            _internalState = internalState;
            _jobManager = jobManager;
            _matchMaker = matchMaker;
            _actionQueue = actionQueue;
            Implementation = implementation;
            Identifier = identifier;
            Location = location;
            var title = $"Load Storage {Identifier}";
            Loading = new JobSlot<SimpleJob>(() => new SimpleJob(Load, title, 1), title, jobManager);
            Loading.StartJob();
        }

        private void Load(CancellationToken cancellationToken)
        {
            // TODO: use cancellationToken
            Implementation.Load();
            _actionQueue.EnQueueAction(() =>
            {
                foreach (KeyValuePair<string, IStorageMod> mod in Implementation.GetMods())
                {
                    var modelMod = new StorageMod(_actionQueue, mod.Value, mod.Key, null, _internalState.GetMod(mod.Key),
                        _jobManager, Identifier, Implementation.CanWrite());
                    Mods.Add(modelMod);
                    ModAdded?.Invoke(modelMod);
                    _matchMaker.AddStorageMod(modelMod);
                }
            });
        }

        public IUpdateState PrepareDownload(IModelRepositoryMod repositoryMod, string identifier)
        {
            // TODO: needs to run synchronized / with callback!
            if (Loading.IsActive()) throw new InvalidOperationException();
            var updateTarget = new UpdateTarget(repositoryMod.GetState().VersionHash.GetHashString(), repositoryMod.Implementation.GetDisplayName());
            var mod = Implementation.CreateMod(identifier);
            var storageMod = new StorageMod(_actionQueue, mod, identifier, updateTarget, _internalState.GetMod(identifier), _jobManager, Identifier, Implementation.CanWrite());
            Mods.Add(storageMod);
            var update = storageMod.PrepareUpdate(repositoryMod, () => RollbackDownload(storageMod));
            ModAdded?.Invoke(storageMod);
            _matchMaker.AddStorageMod(storageMod);
            return update;
        }

        private void RollbackDownload(StorageMod mod)
        {
            _matchMaker.RemoveStorageMod(mod);
            Implementation.RemoveMod(mod.Identifier);
        }

        public bool CanWrite => Implementation.CanWrite();
        public bool IsLoading => Loading.IsActive();

        public override string ToString() => Identifier;

        public event Action<IModelStorageMod> ModAdded;
    }
}