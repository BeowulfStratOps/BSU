using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
        private readonly IModelStructure _modelStructure;
        public IRepository Implementation { get; }
        public string Name { get; }
        public Guid Identifier { get; }
        public string Location { get; }

        private readonly List<IModelRepositoryMod> _mods  = new();

        private readonly Task _loading;

        private readonly ILogger _logger;
        private readonly IErrorPresenter _errorPresenter;

        public Repository(IRepository implementation, string name, string location,
            IRepositoryState internalState, IModelStructure modelStructure, IErrorPresenter errorPresenter)
        {
            _logger = LogHelper.GetLoggerWithIdentifier(this, name);
            _internalState = internalState;
            _modelStructure = modelStructure;
            _errorPresenter = errorPresenter;
            Location = location;
            Implementation = implementation;
            Name = name;
            Identifier = internalState.Identifier;
            _loading = LoadInternal(CancellationToken.None); // TODO: cts
        }

        private async Task LoadInternal(CancellationToken cancellationToken)
        {
            try
            {
                foreach (var (key, mod) in await Implementation.GetMods(cancellationToken))
                {
                    var modelMod = new RepositoryMod(mod, key, _internalState.GetMod(key), _modelStructure, this);
                    _mods.Add(modelMod);
                }
            }
            catch (Exception e)
            {
                _errorPresenter.AddError($"Failed to load repository {Name}.");
                _logger.Error(e);
                throw;
            }
        }

        public async Task<List<IModelRepositoryMod>> GetMods()
        {
            try
            {
                await _loading;
            }
            catch (Exception e)
            {
                return new List<IModelRepositoryMod>();
            }
            return new List<IModelRepositoryMod>(_mods);
        }

        public async Task<CalculatedRepositoryState> GetState(CancellationToken cancellationToken)
        {
            try
            {
                await _loading;
            }
            catch (Exception e)
            {
                return new CalculatedRepositoryState(CalculatedRepositoryStateEnum.Error);
            }

            var mods = await GetMods();

            async Task<(RepositoryModActionSelection selection, ModActionEnum? action)> GetModSelection(IModelRepositoryMod mod)
            {
                var selection = await mod.GetSelection(cancellationToken: cancellationToken);
                var action = selection is not RepositoryModActionStorageMod actionStorageMod
                    ? null
                    : (ModActionEnum?)await CoreCalculation.GetModAction(mod, actionStorageMod.StorageMod, cancellationToken);
                return (selection, action);
            }

            var infoTasks = mods.Select(GetModSelection).ToList();
            await Task.WhenAll(infoTasks);
            var infos = infoTasks.Select(t => t.Result).ToList();

            var calculatedState = CoreCalculation.CalculateRepositoryState(infos);
            _logger.Trace("Repo {0} calculated state: {1}", Identifier, calculatedState);
            return calculatedState;
        }

        public async Task<ServerInfo> GetServerInfo(CancellationToken cancellationToken)
        {
            return await Implementation.GetServerInfo(cancellationToken);
        }
    }
}
