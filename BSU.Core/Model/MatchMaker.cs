using System.Collections.Generic;
using System.Threading.Tasks;
using BSU.CoreCommon;
using NLog;

namespace BSU.Core.Model
{
    internal class MatchMaker
    {
        private readonly List<IModelRepositoryMod> _repositoryMods = new();
        private readonly List<IModelStorageMod> _storageMods = new();
        private bool _storageModsCreated;

        private readonly Logger _logger = EntityLogger.GetLogger();

        public void AddRepositoryMod(IModelRepositoryMod repositoryMod)
        {
            _logger.Debug("Adding repoMod " + repositoryMod.Identifier + ". Loaded: " + repositoryMod.IsLoaded);
            if (!repositoryMod.IsLoaded)
            {
                repositoryMod.OnLoaded += () => AddRepositoryMod(repositoryMod);
                return;
            }
            _repositoryMods.Add(repositoryMod);
            foreach (var storageMod in _storageMods)
            {
                repositoryMod.ProcessMod(storageMod);
            }
            if (_storageModsCreated) repositoryMod.SignalAllStorageModsLoaded();
        }

        public void AddStorageMod(IModelStorageMod storageMod)
        {
            _logger.Debug("Adding storageMod " + storageMod.Identifier);
            _storageMods.Add(storageMod);
            foreach (var repositoryMod in _repositoryMods)
            {
                repositoryMod.ProcessMod(storageMod);
            }
        }

        public void SignalAllStorageModsLoaded()
        {
            _logger.Info("Signaling all storage mods loaded");
            _storageModsCreated = true;
            foreach (var repositoryMod in _repositoryMods)
            {
                repositoryMod.SignalAllStorageModsLoaded();
            }
        }

        public IEnumerable<IModelStorageMod> GetStorageMods() => _storageMods;

        public IEnumerable<IModelRepositoryMod> GetRepositoryMods() => _repositoryMods;
    }
}
