using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BSU.Core.JobManager;
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
        public Uid Uid { get; } = new Uid();
        private readonly List<IModelStorageMod> _mods = new List<IModelStorageMod>();

        private readonly JobSlot _loading;

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
            _loading = new JobSlot(Load, $"Load Storage {Identifier}", jobManager);
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

        public async Task<List<IModelStorageMod>> GetMods()
        {
            await _loading.Do();
            return new List<IModelStorageMod>(_mods);
        }

        public IUpdateState PrepareDownload(IRepositoryMod repositoryMod, UpdateTarget target, string identifier)
        {
            if (_loading.IsRunning) throw new InvalidOperationException(); // TODO: check it is finished. i.e. started and not running
            
            return new StorageModUpdateState(_jobManager, _actionQueue, repositoryMod, target, update =>
            {
                var mod = Implementation.CreateMod(identifier);
                var storageMod = new StorageMod(_actionQueue, mod, identifier, _internalState.GetMod(identifier), _jobManager, Identifier, Implementation.CanWrite());
                _actionQueue.EnQueueAction(() =>
                {
                    _mods.Add(storageMod);
                    ModAdded?.Invoke(storageMod);                   
                });
                return storageMod;
            });
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
