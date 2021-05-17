using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BSU.Core.JobManager;
using BSU.Core.Model.Updating;
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
        private readonly IModelStructure _modelStructure;
        public IRepository Implementation { get; }
        public string Name { get; }
        public event Action<IModelRepositoryMod> ModAdded;
        public Guid Identifier { get; }
        public string Location { get; }

        private readonly List<IModelRepositoryMod> _mods  = new List<IModelRepositoryMod>();

        private readonly AsyncJobSlot _loading;

        private readonly Logger _logger = EntityLogger.GetLogger();

        public Repository(IRepository implementation, string name, string location, IJobManager jobManager,
            IRepositoryState internalState, IActionQueue actionQueue, IModelStructure modelStructure)
        {
            _jobManager = jobManager;
            _internalState = internalState;
            _actionQueue = actionQueue;
            _modelStructure = modelStructure;
            Location = location;
            Implementation = implementation;
            Name = name;
            Identifier = internalState.Identifier;
            var title = $"Load Repo {Identifier}";
            _loading = new AsyncJobSlot(LoadInternal, title, jobManager);
        }

        public async Task Load()
        {
            try
            {
                await _loading.Do();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        private void LoadInternal(CancellationToken cancellationToken)
        {
            try
            {
                // TODO: use cancellationToken
                Implementation.Load();
                foreach (KeyValuePair<string, IRepositoryMod> mod in Implementation.GetMods())
                {
                    var modelMod = new RepositoryMod(_actionQueue, mod.Value, mod.Key, _jobManager, _internalState.GetMod(mod.Key), _modelStructure);
                    modelMod.LocalModUpdated += _ => ReCalculateState();
                    modelMod.SelectionChanged += ReCalculateState;
                    modelMod.OnLoaded += ReCalculateState;
                    _mods.Add(modelMod);
                    _actionQueue.EnQueueAction(() =>
                    {
                        ModAdded?.Invoke(modelMod);
                    });
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        public async Task<List<IModelRepositoryMod>> GetMods()
        {
            await Load();
            return new List<IModelRepositoryMod>(_mods);
        }

        private void ReCalculateState()
        {
            var calculatedState = CoreCalculation.CalculateRepositoryState(_mods, _modelStructure);
            _logger.Trace("Repo {0} calculated state: {1}", Identifier, calculatedState);
            CalculatedStateChanged?.Invoke(calculatedState);
        }

        public event Action<CalculatedRepositoryState> CalculatedStateChanged;

        public RepositoryUpdate DoUpdate()
        {
            var updates = _mods.Select(m => m.DoUpdate()).Where(u => u != null);
            return new RepositoryUpdate(updates.ToList());
        }
    }
}
