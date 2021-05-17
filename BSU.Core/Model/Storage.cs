using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BSU.Core.Hashes;
using BSU.Core.JobManager;
using BSU.Core.Model.Updating;
using BSU.Core.Persistence;
using BSU.CoreCommon;

namespace BSU.Core.Model
{
    internal class Storage : IModelStorage
    {
        private readonly IStorageState _internalState;
        private readonly IJobManager _jobManager;
        public IStorage Implementation { get; }
        public string Name { get; }
        public Guid Identifier { get; }
        public string Location { get; }
        private readonly List<IModelStorageMod> _mods = new();

        private readonly AsyncJobSlot _loading;

        private readonly IActionQueue _actionQueue;

        public Storage(IStorage implementation, string name, string location, IStorageState internalState, IJobManager jobManager, IActionQueue actionQueue)
        {
            _internalState = internalState;
            _jobManager = jobManager;
            _actionQueue = actionQueue;
            Implementation = implementation;
            Name = name;
            Identifier = internalState.Identifier;
            Location = location;
            _loading = new AsyncJobSlot(Load, $"Load Storage {Identifier}", jobManager);
        }

        private void Load(CancellationToken cancellationToken)
        {
            // TODO: use cancellationToken
            Implementation.Load();
            foreach (KeyValuePair<string, IStorageMod> mod in Implementation.GetMods())
            {
                var modelMod = new StorageMod(_actionQueue, mod.Value, mod.Key, _internalState.GetMod(mod.Key),
                    _jobManager, Identifier, Implementation.CanWrite());
                _mods.Add(modelMod);
            }
        }

        public async Task Load()
        {
            var mods = await GetMods();
            foreach (var mod in mods)
            {
                ModAdded?.Invoke(mod);
            }
        }

        public async Task<List<IModelStorageMod>> GetMods()
        {
            await _loading.Do();
            return new List<IModelStorageMod>(_mods);
        }

        public IUpdateCreate PrepareDownload(IRepositoryMod repositoryMod, UpdateTarget target, string identifier,
            Action<IModelStorageMod> createdCallback, MatchHash matchHash, VersionHash versionHash)
        {
            if (!_loading.IsDone) throw new InvalidOperationException();

            return new StorageModUpdateState(_jobManager, repositoryMod, target, update =>
            {
                var mod = Implementation.CreateMod(identifier);
                var state = _internalState.GetMod(identifier);
                state.UpdateTarget = target;
                var storageMod = new StorageMod(_actionQueue, mod, identifier, state, _jobManager, Identifier, Implementation.CanWrite(), update);
                _actionQueue.EnQueueAction(() =>
                {
                    _mods.Add(storageMod);
                    ModAdded?.Invoke(storageMod);
                    createdCallback(storageMod);
                });
                return storageMod;
            }, matchHash, versionHash);
        }

        public bool CanWrite => Implementation.CanWrite();
        public PersistedSelection AsStorageIdentifier()
        {
            return new PersistedSelection(Identifier, null);
        }

        public async Task<bool> HasMod(string downloadIdentifier)
        {
            await _loading.Do();
            // TODO: meh?
            return _mods.Any(m => m.GetStorageModIdentifiers().Mod == downloadIdentifier);
        }

        public event Action<IModelStorageMod> ModAdded;
    }
}
