using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BSU.Core.Persistence;
using BSU.CoreCommon;

namespace BSU.Core.Model
{
    internal class Storage : IModelStorage
    {
        private readonly IStorageState _internalState;
        public IStorage Implementation { get; }
        public string Name { get; }
        public Guid Identifier { get; }
        public string Location { get; }
        private readonly List<IModelStorageMod> _mods = new();

        private readonly Task _loading;

        public Storage(IStorage implementation, string name, string location, IStorageState internalState)
        {
            _internalState = internalState;
            Implementation = implementation;
            Name = name;
            Identifier = internalState.Identifier;
            Location = location;
            _loading = Load(CancellationToken.None); // TODO: cts, task.run?
        }

        private async Task Load(CancellationToken cancellationToken)
        {
            foreach (KeyValuePair<string, IStorageMod> mod in await Implementation.GetMods(cancellationToken))
            {
                var modelMod = new StorageMod(mod.Value, mod.Key, _internalState.GetMod(mod.Key),
                    this, Implementation.CanWrite());
                _mods.Add(modelMod);
                ModAdded?.Invoke(modelMod);
            }
        }

        public async Task<List<IModelStorageMod>> GetMods()
        {
            await _loading;
            return new List<IModelStorageMod>(_mods);
        }

        public async Task<IModelStorageMod> CreateMod(string identifier, UpdateTarget updateTarget)
        {
            await _loading;
            var mod = await Implementation.CreateMod(identifier, CancellationToken.None);
            var state = _internalState.GetMod(identifier);
            state.UpdateTarget = updateTarget;
            var storageMod = new StorageMod(mod, identifier, state, this, true);
            _mods.Add(storageMod);
            return storageMod;
        }

        public bool CanWrite => Implementation.CanWrite();
        public PersistedSelection AsStorageIdentifier()
        {
            return new PersistedSelection(PersistedSelectionType.Download, Identifier, null);
        }

        public async Task<bool> HasMod(string downloadIdentifier)
        {
            await _loading;
            // TODO: meh?
            return _mods.Any(m => m.GetStorageModIdentifiers().Mod == downloadIdentifier);
        }

        public event Action<IModelStorageMod> ModAdded;
    }
}
