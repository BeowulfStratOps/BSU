using System.Collections.Generic;
using System.Threading.Tasks;

namespace BSU.Core.Model
{
    internal class MatchMaker
    {
        private readonly List<IModelRepositoryMod> _repositoryMods = new();
        private readonly List<IModelStorageMod> _storageMods = new();
        private bool _storageModsCreated;

        public async Task AddRepoMod(IModelRepositoryMod repositoryMod)
        {
            _repositoryMods.Add(repositoryMod);
            foreach (var storageMod in _storageMods)
            {
                await repositoryMod.ProcessMod(storageMod);
            }
            if (_storageModsCreated) await repositoryMod.SignalAllStorageModsLoaded();
        }

        public async Task AddStorageMod(IModelStorageMod storageMod)
        {
            _storageMods.Add(storageMod);
            foreach (var repositoryMod in _repositoryMods)
            {
                await repositoryMod.ProcessMod(storageMod);
            }
        }

        public async Task SignalAllStorageModsLoaded()
        {
            _storageModsCreated = true;
            foreach (var repositoryMod in _repositoryMods)
            {
                await repositoryMod.SignalAllStorageModsLoaded();
            }
        }
    }
}
