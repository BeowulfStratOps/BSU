using System;
using System.Collections.Generic;
using System.Threading;
using BSU.Core.JobManager;
using BSU.CoreCommon;

namespace BSU.Core.Model
{
    internal class Repository
    {
        private readonly IJobManager _jobManager;
        private readonly IMatchMaker _matchMaker;
        private readonly IInternalState _internalState;
        public IRepository Implementation { get; }
        public string Identifier { get; }
        public string Location { get; }
        public Uid Uid { get; } = new Uid();

        public List<RepositoryMod> Mods { get; } = new List<RepositoryMod>();
        
        public JobSlot<SimpleJob> Loading { get; }

        internal Model Model { get; }

        public Repository(IRepository implementation, string identifier, string location, IJobManager jobManager, IMatchMaker matchMaker, IInternalState internalState, Model model)
        {
            _jobManager = jobManager;
            _matchMaker = matchMaker;
            _internalState = internalState;
            Model = model;
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
            Model.EnQueueAction(() =>
            {
                foreach (KeyValuePair<string, IRepositoryMod> mod in Implementation.GetMods())
                {
                    var modelMod = new RepositoryMod(this, mod.Value, mod.Key, _jobManager, _internalState);
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

        private CalculatedRepositoryState _calculatedState = CalculatedRepositoryState.Loading;

        public CalculatedRepositoryState CalculatedState
        {
            get => _calculatedState;
            private set
            {
                if (value == _calculatedState) return;
                _calculatedState = value;
                CalculatedStateChanged?.Invoke();
            }
        }

        private void ReCalculateState()
        {
            CalculatedState = CoreCalculation.CalculateRepositoryState(Mods);
        }

        public event Action CalculatedStateChanged;

        public event Action<RepositoryMod> ModAdded;
    }
}