using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BSU.Core.Concurrency;
using BSU.Core.Ioc;
using BSU.Core.Launch;
using BSU.Core.Model.Updating;
using BSU.Core.Persistence;
using BSU.Core.Sync;
using BSU.CoreCommon;
using NLog;

namespace BSU.Core.Model
{
    internal class Repository : IModelRepository
    {
        private readonly IRepositoryState _internalState;
        private readonly IServiceProvider _services;
        public IRepository Implementation { get; }
        public string Name { get; }
        public Guid Identifier { get; }
        public string Location { get; }

        private LoadingState _state = LoadingState.Loading;
        public event Action<IModelRepository>? StateChanged;
        public GameLaunchResult Launch() => GameLauncher.Launch(this, _eventBus);

        private List<IModelRepositoryMod>? _mods;

        private readonly ILogger _logger;
        private readonly IErrorPresenter _errorPresenter;
        private ServerInfo? _serverInfo;
        private readonly IEventBus _eventBus;

        public Repository(IRepository implementation, string name, string location,
            IRepositoryState internalState, IServiceProvider services)
        {
            _logger = LogHelper.GetLoggerWithIdentifier(this, name);
            _internalState = internalState;
            _services = services;
            _errorPresenter = services.Get<IErrorPresenter>();
            _eventBus = services.Get<IEventBus>();
            Location = location;
            Implementation = implementation;
            Name = name;
            Identifier = internalState.Identifier;
            Load();
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

        public PresetSettings Settings
        {
            get
            {
                var settings = _internalState.GetSettings();
                if (settings != null) return settings;
                settings =PresetSettings.BuildDefault();
                _internalState.SetSettings(settings);
                return settings;
            }
            set => _internalState.SetSettings(value);
        }

        private async Task<(Dictionary<string, IRepositoryMod> mods, ServerInfo serverInfo)> LoadAsync()
        {
            var mods = await Implementation.GetMods(CancellationToken.None);
            var serverInfo = await Implementation.GetServerInfo(CancellationToken.None);
            return (mods, serverInfo);
        }

        private void Load()
        {
            Task.Run(LoadAsync).ContinueInEventBus(_eventBus, getResult =>
            {
                try
                {
                    (var mods, _serverInfo) = getResult();

                    _mods = new List<IModelRepositoryMod>();
                    foreach (var (key, mod) in mods)
                    {
                        var modelMod = new RepositoryMod(mod, key, _internalState.GetMod(key), this, _services);
                        _mods.Add(modelMod);
                    }

                    State = LoadingState.Loaded;
                }
                catch (Exception e)
                {
                    _errorPresenter.AddError($"Failed to load repository {Name}.");
                    _logger.Error(e);
                    State = LoadingState.Error;
                }
            });
        }

        public List<IModelRepositoryMod> GetMods()
        {
            if (State != LoadingState.Loaded) throw new InvalidOperationException($"Not allowed in state {State}");
            return _mods!;
        }

        public ServerInfo GetServerInfo()
        {
            if (State != LoadingState.Loaded) throw new InvalidOperationException($"Not allowed in state {State}");
            return _serverInfo!;
        }
    }

    internal enum LoadingState
    {
        Loading,
        Loaded,
        Error
    }
}
