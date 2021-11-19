﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BSU.Core.Concurrency;
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
        public IRepository Implementation { get; }
        public string Name { get; }
        public Guid Identifier { get; }
        public string Location { get; }

        private LoadingState _state = LoadingState.Loading;
        public event Action<IModelRepository> StateChanged;

        private List<IModelRepositoryMod> _mods;

        private readonly ILogger _logger;
        private readonly IErrorPresenter _errorPresenter;
        private ServerInfo _serverInfo;

        public Repository(IRepository implementation, string name, string location,
            IRepositoryState internalState, IErrorPresenter errorPresenter)
        {
            _logger = LogHelper.GetLoggerWithIdentifier(this, name);
            _internalState = internalState;
            _errorPresenter = errorPresenter;
            Location = location;
            Implementation = implementation;
            Name = name;
            Identifier = internalState.Identifier;
            Load();
        }

        public LoadingState State
        {
            get => _state;
            set
            {
                if (_state == value) return;
                _logger.Debug($"Changing state from {_state} to {value}");
                _state = value;
                StateChanged?.Invoke(this);
            }
        }

        private async Task<(Dictionary<string, IRepositoryMod> mods, ServerInfo serverInfo)> LoadAsync()
        {
            var mods = await Implementation.GetMods(CancellationToken.None);
            var serverInfo = await Implementation.GetServerInfo(CancellationToken.None);
            return (mods, serverInfo);
        }

        private void Load()
        {
            Task.Run(LoadAsync).ContinueInCurrentContext(getResult =>
            {
                try
                {
                    (var mods, _serverInfo) = getResult();

                    _mods = new List<IModelRepositoryMod>();
                    foreach (var (key, mod) in mods)
                    {
                        var modelMod = new RepositoryMod(mod, key, _internalState.GetMod(key), this);
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

        public List<IModelRepositoryMod> GetMods() => _mods;

        public ServerInfo GetServerInfo()
        {
            return _serverInfo;
        }
    }

    internal enum LoadingState
    {
        Loading,
        Loaded,
        Error
    }
}
