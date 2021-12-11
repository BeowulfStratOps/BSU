using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BSU.Core.Concurrency;
using BSU.Core.Hashes;
using BSU.Core.Persistence;
using BSU.CoreCommon;
using NLog;

namespace BSU.Core.Model
{
    internal class Storage : IModelStorage
    {
        private readonly IStorageState _internalState;
        public IStorage Implementation { get; }
        public string Name { get; }
        public bool IsDeleted { get; private set; }
        public Guid Identifier { get; }
        public string Location { get; }
        private List<IModelStorageMod>? _mods; // TODO: use a property to block access while state is invalid
        private readonly IErrorPresenter _errorPresenter;
        private readonly ILogger _logger;
        private LoadingState _state = LoadingState.Loading;
        private readonly IEventBus _eventBus;

        public event Action<IModelStorage>? StateChanged;
        public event Action<IModelStorageMod>? AddedMod;

        public Storage(IStorage implementation, string name, string location, IStorageState internalState, IErrorPresenter errorPresenter, IEventBus eventBus)
        {
            _logger = LogHelper.GetLoggerWithIdentifier(this, name);
            _internalState = internalState;
            _errorPresenter = errorPresenter;
            _eventBus = eventBus;
            Implementation = implementation;
            Name = name;
            Identifier = internalState.Identifier;
            Location = location;
            Load();
        }

        private async Task<Dictionary<string, IStorageMod>> LoadAsync()
        {
            return await Implementation.GetMods(CancellationToken.None);
        }

        public LoadingState State
        {
            get => _state;
            private set
            {
                if (_state == value) return;
                _logger.Debug($"Changing state from {_state} to {value}");
                _state = value;
                StateChanged?.Invoke(this);
            }
        }

        private void Load()
        {
            Task.Run(LoadAsync).ContinueInEventBus(_eventBus, getResult =>
            {
                try
                {
                    _mods = new List<IModelStorageMod>();
                    foreach (var (identifier, implementation) in getResult())
                    {
                        var modelMod = new StorageMod(implementation, identifier, _internalState.GetMod(identifier),
                            this, Implementation.CanWrite(), _eventBus);
                        _mods.Add(modelMod);
                    }

                    State = LoadingState.Loaded;
                }
                catch (DirectoryNotFoundException e)
                {
                    _errorPresenter.AddError(
                        $"Failed to load storage {Name}, directory '{Location}' could not be found.");
                    _logger.Error(e);
                    _state = LoadingState.Error;
                }
                catch (Exception e)
                {
                    _errorPresenter.AddError($"Failed to load storage '{Name}'.");
                    _logger.Error(e);
                    _state = LoadingState.Error;
                }
            });
        }

        public List<IModelStorageMod> GetMods()
        {
            if (State != LoadingState.Loaded) throw new InvalidOperationException($"Not allowed in State {State}");
            return new List<IModelStorageMod>(_mods!);
        }

        public async Task<IModelStorageMod> CreateMod(string identifier, UpdateTarget updateTarget, MatchHash createMatchHash)
        {
            if (State != LoadingState.Loaded) throw new InvalidOperationException($"Not allowed in State {State}");
            var mod = await Implementation.CreateMod(identifier, CancellationToken.None);
            var state = _internalState.GetMod(identifier);
            state.UpdateTarget = updateTarget;
            var storageMod = new StorageMod(mod, identifier, state, this, true, _eventBus, createMatchHash);
            _mods!.Add(storageMod);
            AddedMod?.Invoke(storageMod);
            return storageMod;
        }

        public bool CanWrite => Implementation.CanWrite();
        public PersistedSelection AsStorageIdentifier()
        {
            return new PersistedSelection(PersistedSelectionType.Download, Identifier, null);
        }

        public bool HasMod(string downloadIdentifier)
        {
            if (State != LoadingState.Loaded) throw new InvalidOperationException($"Not allowed in State {State}");
            return _mods!.Any(m => string.Equals(m.Identifier, downloadIdentifier, StringComparison.InvariantCultureIgnoreCase));
        }

        public string GetLocation() => Location;
        public bool IsAvailable() => _state != LoadingState.Error;

        public void Delete(bool removeMods)
        {
            if (State != LoadingState.Loaded) throw new InvalidOperationException($"Not allowed in State {State}");
            IsDeleted = true;
            foreach (var mod in _mods!)
            {
                mod.Delete(removeMods);
            }
        }
    }
}
