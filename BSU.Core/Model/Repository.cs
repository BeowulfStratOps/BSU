using System;
using System.Collections.Generic;
using System.Threading;
using BSU.Core.JobManager;
using BSU.Core.Persistence;
using BSU.CoreCommon;

namespace BSU.Core.Model
{
    internal class Repository : IModelRepository
    {
        private readonly IJobManager _jobManager;
        private readonly IMatchMaker _matchMaker;
        private readonly IRepositoryState _internalState;
        private readonly IActionQueue _actionQueue;
        private readonly RelatedActionsBag _relatedActionsBag;
        private readonly IModelStructure _modelStructure;
        public IRepository Implementation { get; }
        public string Identifier { get; }
        public string Location { get; }
        public Uid Uid { get; } = new Uid();

        public List<IModelRepositoryMod> Mods { get; } = new List<IModelRepositoryMod>();
        
        public JobSlot<SimpleJob> Loading { get; }

        public Repository(IRepository implementation, string identifier, string location, IJobManager jobManager,
            IMatchMaker matchMaker, IRepositoryState internalState, IActionQueue actionQueue,
            RelatedActionsBag relatedActionsBag, IModelStructure modelStructure)
        {
            _jobManager = jobManager;
            _matchMaker = matchMaker;
            _internalState = internalState;
            _actionQueue = actionQueue;
            _relatedActionsBag = relatedActionsBag;
            _modelStructure = modelStructure;
            Location = location;
            Implementation = implementation;
            Identifier = identifier;
            var title = $"Load Repo {Identifier}";
            Loading = new JobSlot<SimpleJob>(() => new SimpleJob(Load, title, 1), title, jobManager);
            Loading.StartJob();
        }

        private void Load(CancellationToken cancellationToken)
        {
            // TODO: use cancellationToken
            Implementation.Load();
            _actionQueue.EnQueueAction(() =>
            {
                foreach (KeyValuePair<string, IRepositoryMod> mod in Implementation.GetMods())
                {
                    var modelMod = new RepositoryMod(_actionQueue, mod.Value, mod.Key, _jobManager, _internalState.GetMod(mod.Key), _relatedActionsBag, _modelStructure);
                    modelMod.ActionAdded += storageMod =>
                    {
                        modelMod.Actions[storageMod].Updated += ReCalculateState;
                    };
                    Mods.Add(modelMod);
                    ModAdded?.Invoke(modelMod);
                    _matchMaker.AddRepositoryMod(modelMod);
                }
            });
        }

        private CalculatedRepositoryState _calculatedState = new CalculatedRepositoryState(CalculatedRepositoryStateEnum.Loading, false);

        public CalculatedRepositoryState CalculatedState
        {
            get => _calculatedState;
            private set
            {
                if (value.Equals(_calculatedState)) return;
                _calculatedState = value;
                CalculatedStateChanged?.Invoke();
            }
        }

        private void ReCalculateState()
        {
            CalculatedState = CoreCalculation.CalculateRepositoryState(Mods);
        }

        public event Action CalculatedStateChanged;
        
        public bool IsLoading => Loading.IsActive();

        public event Action<IModelRepositoryMod> ModAdded;
    }
}