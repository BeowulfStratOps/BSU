using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using BSU.Core.JobManager;
using BSU.Core.View;
using BSU.CoreCommon;

namespace BSU.Core.Model
{
    internal class Repository
    {
        private readonly IJobManager _jobManager;
        private readonly MatchMaker _matchMaker;
        private readonly IInternalState _internalState;
        public IRepository Implementation { get; }
        public string Identifier { get; }
        public string Location { get; }
        public Uid Uid { get; } = new Uid();

        public List<RepositoryMod> Mods { get; } = new List<RepositoryMod>();
        
        public JobSlot<SimpleJob> Loading { get; }

        internal Model Model { get; }

        public Repository(IRepository implementation, string identifier, string location, IJobManager jobManager, MatchMaker matchMaker, IInternalState internalState, Model model)
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
                    Mods.Add(modelMod);
                    ModAdded?.Invoke(modelMod);
                    _matchMaker.AddRepositoryMod(modelMod);
                }
            });
        }

        private CalculatedRepositoryState _calculatedState = CalculatedRepositoryState.Loading;

        public CalculatedRepositoryState CalculatedState
        {
            get
            {
                return _calculatedState;
            }
            private set
            {
                if (value == _calculatedState) return;
                _calculatedState = value;
                CalculatedStateChanged?.Invoke();
            }
        }

        internal void ReCalculateState()
        {
            /*
            Loading, // 3. At least one loading
            NeedsUpdate, // 2. all selected, no internal conflicts. 
            NeedsDowload, // 2. more than 50% of the mods need a download, otherwise same as update
            Ready, // 1. All use
            RequiresUserIntervention // Else
            */

            if (Mods.All(mod =>
                mod.SelectedStorageMod != null && mod.Actions[mod.SelectedStorageMod].ActionType == ModActionEnum.Use))
            {
                CalculatedState = CalculatedRepositoryState.Ready;
                return;
            }

            if (Mods.All(mod => mod.SelectedStorageMod != null || mod.SelectedDownloadStorage != null))
            {
                // No internal conflicts
                if (Mods.Where(mod => mod.SelectedStorageMod != null).All(mod =>
                    mod.Actions[mod.SelectedStorageMod].Conflicts.All(conflict => conflict.Parent.Repository != this)))
                {
                    if (Mods.Count(mod => mod.SelectedDownloadStorage != null) > 0.5 * Mods.Count)
                        CalculatedState = CalculatedRepositoryState.NeedsDowload;
                    else
                        CalculatedState = CalculatedRepositoryState.NeedsUpdate;
                    return;
                }
            }

            if (Mods.All(mod =>
                mod.SelectedStorageMod == null && mod.SelectedDownloadStorage == null &&
                mod.Actions.Any(kv => kv.Value.ActionType == ModActionEnum.Loading)))
            {
                CalculatedState = CalculatedRepositoryState.Loading;
                return;
            }

            CalculatedState = CalculatedRepositoryState.RequiresUserIntervention;
        }

        public event Action CalculatedStateChanged;

        public event Action<RepositoryMod> ModAdded;
    }
}