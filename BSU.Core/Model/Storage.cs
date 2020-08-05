using System;
using System.Collections.Generic;
using System.Threading;
using BSU.Core.JobManager;
using BSU.Core.Sync;
using BSU.Core.View;
using BSU.CoreCommon;

namespace BSU.Core.Model
{
    internal class Storage
    {
        private readonly IInternalState _internalState;
        private readonly IJobManager _jobManager;
        private readonly MatchMaker _matchMaker;
        public IStorage Implementation { get; }
        public string Identifier { get; }
        public string Location { get; }
        public Uid Uid { get; } = new Uid();
        public List<StorageMod> Mods { private set; get; } = new List<StorageMod>(); // TODO: readonly

        public JobSlot<SimpleJob> Loading { get; }
        
        internal Model Model { get; }

        public Storage(IStorage implementation, string identifier, string location, IInternalState internalState, IJobManager jobManager, MatchMaker matchMaker, Model model)
        {
            _internalState = internalState;
            _jobManager = jobManager;
            _matchMaker = matchMaker;
            Model = model;
            Implementation = implementation;
            Identifier = identifier;
            Location = location;
            var title = $"Load Storage {Identifier}";
            Loading = new JobSlot<SimpleJob>(() => new SimpleJob(Load, title, 1), title, jobManager);
            Loading.StartJob();
        }

        private void Load(CancellationToken cancellationToken)
        {
            // TODO: use cancellationToken
            Implementation.Load();
            Model.EnQueueAction(() =>
            {
                foreach (KeyValuePair<string, IStorageMod> mod in Implementation.GetMods())
                {
                    var modelMod = new StorageMod(this, mod.Value, mod.Key, null, _internalState, _jobManager);
                    Mods.Add(modelMod);
                    ModAdded?.Invoke(modelMod);
                    _matchMaker.AddStorageMod(modelMod);
                }
            });
        }

        internal IUpdateState PrepareDownload(RepositoryMod repositoryMod, string identifier)
        {
            if (Loading.IsActive()) throw new InvalidOperationException();
            // TODO: state lock? for this? for repo mod?
            var updateTarget = new UpdateTarget(repositoryMod.GetState().VersionHash.GetHashString(), repositoryMod.Implementation.GetDisplayName());
            var mod = Implementation.CreateMod(identifier);
            var storageMod = new StorageMod(this, mod, identifier, updateTarget, _internalState, _jobManager);
            Mods.Add(storageMod);
            var update = storageMod.PrepareUpdate(repositoryMod, () => RollbackDownload(storageMod));
            ModAdded?.Invoke(storageMod);
            _matchMaker.AddStorageMod(storageMod);
            return update;
        }

        private void RollbackDownload(StorageMod mod)
        {
            _matchMaker.RemoveStorageMod(mod);
            Implementation.RemoveMod(mod.Identifier);
        }
        
        public event Action<StorageMod> ModAdded;
    }
}