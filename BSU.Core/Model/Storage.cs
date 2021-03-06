﻿using System;
using System.Collections.Generic;
using System.Linq;
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
        public string Name { get; }
        public Guid Identifier { get; }
        public string Location { get; }
        public Uid Uid { get; } = new Uid();
        public List<IModelStorageMod> Mods { private set; get; } = new List<IModelStorageMod>(); // TODO: readonly

        public JobSlot<SimpleJob> Loading { get; }

        private readonly IActionQueue _actionQueue;

        public Storage(IStorage implementation, string name, string location, IStorageState internalState, IJobManager jobManager, IMatchMaker matchMaker, IActionQueue actionQueue)
        {
            _internalState = internalState;
            _jobManager = jobManager;
            _matchMaker = matchMaker;
            _actionQueue = actionQueue;
            Implementation = implementation;
            Name = name;
            Identifier = internalState.Identifier;
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

        public IUpdateState PrepareDownload(IRepositoryMod repositoryMod, UpdateTarget target, string identifier)
        {
            if (Loading.IsActive()) throw new InvalidOperationException();

            return new StorageModUpdateState(_jobManager, _actionQueue, repositoryMod, target, update =>
            {
                var mod = Implementation.CreateMod(identifier);
                var storageMod = new StorageMod(_actionQueue, mod, identifier, target, _internalState.GetMod(identifier), _jobManager, Identifier, Implementation.CanWrite(), update);
                Mods.Add(storageMod);
                ModAdded?.Invoke(storageMod);
                _matchMaker.AddStorageMod(storageMod);
                return storageMod;
            });
        }

        public bool CanWrite => Implementation.CanWrite();
        public bool IsLoading => Loading.IsActive();
        public PersistedSelection GetStorageIdentifier()
        {
            return new PersistedSelection(Identifier, null);
        }

        public bool HasMod(string downloadIdentifier)
        {
            // TODO: meh?
            return Mods.Any(m => m.GetStorageModIdentifiers().Mod == downloadIdentifier);
        }

        public event Action<IModelStorageMod> ModAdded;
    }
}
