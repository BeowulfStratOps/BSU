using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BSU.Core.Model.Updating;
using BSU.Core.Persistence;
using BSU.CoreCommon;
using NLog;

namespace BSU.Core.Model
{
    internal class Repository : IModelRepository
    {
        private readonly IRepositoryState _internalState;
        private readonly IModelStructure _modelStructure;
        public IRepository Implementation { get; }
        public string Name { get; }
        public Guid Identifier { get; }
        public string Location { get; }

        private readonly List<IModelRepositoryMod> _mods  = new();

        private readonly Task _loading;

        private readonly Logger _logger = EntityLogger.GetLogger();

        public Repository(IRepository implementation, string name, string location,
            IRepositoryState internalState, IModelStructure modelStructure)
        {
            _internalState = internalState;
            _modelStructure = modelStructure;
            Location = location;
            Implementation = implementation;
            Name = name;
            Identifier = internalState.Identifier;
            _loading = LoadInternal(CancellationToken.None); // TODO: cts
        }

        private async Task LoadInternal(CancellationToken cancellationToken)
        {
            foreach (var (key, mod) in await Implementation.GetMods(cancellationToken))
            {
                var modelMod = new RepositoryMod(mod, key, _internalState.GetMod(key), _modelStructure);
                _mods.Add(modelMod);
            }
        }

        public async Task<List<IModelRepositoryMod>> GetMods()
        {
            await _loading;
            return new List<IModelRepositoryMod>(_mods);
        }

        public async Task<CalculatedRepositoryState> GetState(CancellationToken cancellationToken)
        {
            var mods = await GetMods();

            async Task<(IModelRepositoryMod mod, RepositoryModActionSelection selection, ModActionEnum? action)> GetModSelection(IModelRepositoryMod mod)
            {
                var selection = await mod.GetSelection(cancellationToken);
                var action = selection.StorageMod == null
                    ? null
                    : (ModActionEnum?)await CoreCalculation.GetModAction(mod, selection.StorageMod, cancellationToken);
                return (mod, selection, action);
            }

            var infoTasks = mods.Select(GetModSelection).ToList();
            await Task.WhenAll(infoTasks);
            var infos = infoTasks.Select(t => t.Result).ToList();

            var calculatedState = CoreCalculation.CalculateRepositoryState(infos);
            _logger.Trace("Repo {0} calculated state: {1}", Identifier, calculatedState);
            return calculatedState;
        }

        public async Task<RepositoryUpdate> DoUpdate(CancellationToken cancellationToken)
        {
            var updateTasks = _mods.Select(m => m.StartUpdate(cancellationToken)).ToList();
            await Task.WhenAll(updateTasks);
            var updates = updateTasks.Select(ut => ut.Result).Where(u => u != null).ToList();

            return new RepositoryUpdate(updates);
        }
    }
}
