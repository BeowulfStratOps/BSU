using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BSU.Core.JobManager;
using BSU.Core.Persistence;
using BSU.CoreCommon;
using NLog;

namespace BSU.Core.Model
{
    internal class Repository : IModelRepository
    {
        private readonly IJobManager _jobManager;
        private readonly IRepositoryState _internalState;
        private readonly IActionQueue _actionQueue;
        private readonly RelatedActionsBag _relatedActionsBag;
        private readonly IModelStructure _modelStructure;
        public IRepository Implementation { get; }
        public string Name { get; }
        public event Action<IModelRepositoryMod> ModAdded;
        public Guid Identifier { get; }
        public string Location { get; }
        public Uid Uid { get; } = new Uid();

        private readonly List<IModelRepositoryMod> _mods  = new List<IModelRepositoryMod>();

        private readonly JobSlot _loading;

        private readonly Logger _logger = LogManager.GetCurrentClassLogger();

        public RepositoryUpdate CurrentUpdate
        {
            get => _currentUpdate;
            private set
            {
                if (_currentUpdate == value) return;
                _currentUpdate = value;
                OnUpdateChange?.Invoke();
            }
        }

        public event Action OnUpdateChange;
        public async Task ProcessMods(List<IModelStorage> storages)
        {
            var mods = await GetMods();
            await Task.WhenAll(mods.Select(m => m.ProcessMods(storages)));
            // TODO: calculate state
        }

        public Repository(IRepository implementation, string name, string location, IJobManager jobManager,
            IRepositoryState internalState, IActionQueue actionQueue,
            RelatedActionsBag relatedActionsBag, IModelStructure modelStructure)
        {
            _jobManager = jobManager;
            _internalState = internalState;
            _actionQueue = actionQueue;
            _relatedActionsBag = relatedActionsBag;
            _modelStructure = modelStructure;
            Location = location;
            Implementation = implementation;
            Name = name;
            Identifier = internalState.Identifier;
            var title = $"Load Repo {Identifier}";
            _loading = new JobSlot(LoadInternal, title, jobManager);
        }

        private async Task Load()
        {
            await _loading.Do();
        }

        private void LoadInternal(CancellationToken cancellationToken)
        {
            // TODO: use cancellationToken
            Implementation.Load();
            foreach (KeyValuePair<string, IRepositoryMod> mod in Implementation.GetMods())
            {
                var modelMod = new RepositoryMod(_actionQueue, mod.Value, mod.Key, _jobManager, _internalState.GetMod(mod.Key), _relatedActionsBag, _modelStructure);
                modelMod.LocalModAdded += storageMod =>
                {
                    modelMod.LocalMods[storageMod].Updated += ReCalculateState;
                    ReCalculateState();
                };
                modelMod.SelectionChanged += ReCalculateState;
                _mods.Add(modelMod);
                _actionQueue.EnQueueAction(() =>
                {
                    ModAdded?.Invoke(modelMod);
                });
            }
        }

        private CalculatedRepositoryState _calculatedState = new CalculatedRepositoryState(CalculatedRepositoryStateEnum.Loading, false);
        private RepositoryUpdate _currentUpdate;

        public async Task<List<IModelRepositoryMod>> GetMods()
        {
            await Load();
            return new List<IModelRepositoryMod>(_mods);
        }

        public CalculatedRepositoryState CalculatedState
        {
            get => _calculatedState;
            private set
            {
                if (value == _calculatedState) return;
                _logger.Trace("Repo {0} State changing from {1} to {2}", Identifier, _calculatedState, value);
                _calculatedState = value;
                CalculatedStateChanged?.Invoke(value);
            }
        }

        private void ReCalculateState()
        {
            CalculatedState = CoreCalculation.CalculateRepositoryState(_mods);
            _logger.Trace("Repo {0} calculated state: {1}", Identifier, CalculatedState);
        }

        public event Action<CalculatedRepositoryState> CalculatedStateChanged;

        public RepositoryUpdate DoUpdate()
        {
            if (CurrentUpdate != null) throw new InvalidOperationException();
            
            var repoUpdate = new RepositoryUpdate();

            foreach (var mod in _mods)
            {
                mod.DoUpdate();
                var updateInfo = mod.CurrentUpdate;
                if (updateInfo == null) continue; // Do nothing
                repoUpdate.Add(updateInfo);
            }

            repoUpdate.OnEnded += () => CurrentUpdate = null;
            CurrentUpdate = repoUpdate;
            return repoUpdate;
        }
    }
}
