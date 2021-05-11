using System.Collections.Generic;
using System.Threading.Tasks;

namespace BSU.Core.Model
{
    internal class MatchMaker
    {
        private readonly List<IModelRepositoryMod> _repositoryMods = new();
        private readonly List<IModelStorageMod> _storageMods = new();
        private bool _storageModsCreated;

        public void AddRepoMod(IModelRepositoryMod repositoryMod)
        {
            if (!repositoryMod.IsLoaded)
            {
                repositoryMod.OnLoaded += () => AddRepoMod(repositoryMod);
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
            _storageMods.Add(storageMod);
            foreach (var repositoryMod in _repositoryMods)
            {
                repositoryMod.ProcessMod(storageMod);
            }
        }

        public void SignalAllStorageModsLoaded()
        {
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
