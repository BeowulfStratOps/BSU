using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BSU.Core.Persistence;
using BSU.CoreCommon;
using NLog;

namespace BSU.Core.Model
{
    internal class Storage : IModelStorage
    {
        private readonly IStorageState _internalState;
        private readonly IModelStructure _modelStructure;
        public IStorage Implementation { get; }
        public string Name { get; }
        public Guid Identifier { get; }
        public string Location { get; }
        private readonly List<IModelStorageMod> _mods = new();
        private readonly IErrorPresenter _errorPresenter;
        private readonly ILogger _logger;

        private readonly Task _loading;

        public Storage(IStorage implementation, string name, string location, IStorageState internalState, IModelStructure modelStructure, IErrorPresenter errorPresenter)
        {
            _logger = LogHelper.GetLoggerWithIdentifier(this, name);
            _internalState = internalState;
            _modelStructure = modelStructure;
            _errorPresenter = errorPresenter;
            Implementation = implementation;
            Name = name;
            Identifier = internalState.Identifier;
            Location = location;
            _loading = Load(CancellationToken.None); // TODO: cts, task.run?
        }

        private async Task Load(CancellationToken cancellationToken)
        {
            try
            {
                foreach (var (identifier, implementation) in await Implementation.GetMods(cancellationToken))
                {
                    var modelMod = new StorageMod(implementation, identifier, _internalState.GetMod(identifier),
                        this, Implementation.CanWrite(), _modelStructure);
                    _mods.Add(modelMod);
                    ModAdded?.Invoke(modelMod);
                }
            }
            catch (DirectoryNotFoundException e)
            {
                _errorPresenter.AddError($"Failed to load storage {Name}, directory '{Location}' could not be found.");
                _logger.Error(e);
                throw;
            }
            catch (Exception e)
            {
                _errorPresenter.AddError($"Failed to load storage {Name}.");
                _logger.Error(e);
                throw;
            }
        }

        public async Task<List<IModelStorageMod>> GetMods()
        {
            try
            {
                await _loading;
            }
            catch (Exception e)
            {
                return new List<IModelStorageMod>();
            }
            return new List<IModelStorageMod>(_mods);
        }

        public async Task<IModelStorageMod> CreateMod(string identifier, UpdateTarget updateTarget)
        {
            await _loading;
            var mod = await Implementation.CreateMod(identifier, CancellationToken.None);
            var state = _internalState.GetMod(identifier);
            state.UpdateTarget = updateTarget;
            var storageMod = new StorageMod(mod, identifier, state, this, true, _modelStructure);
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

        public string GetLocation() => Location;
        public async Task<bool> IsAvailable()
        {
            try
            {
                await _loading;
                return true;
            }
            catch (Exception e)
            {
                return false;
            }
        }

        public event Action<IModelStorageMod> ModAdded;
    }
}
