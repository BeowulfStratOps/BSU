using System;
using System.Collections.Generic;
using BSU.Core.Hashes;

namespace BSU.Core.Model
{
    internal class MatchMaker
    {
        private readonly List<RepositoryMod> _repoMods = new List<RepositoryMod>();
        private readonly List<StorageMod> _storageMods = new List<StorageMod>();
        private readonly object _lock = new object();

        // TODO: add check for started updates!

        public void AddStorageMod(StorageMod storageMod)
        {
            lock (_lock)
            {
                if (_storageMods.Contains(storageMod)) return;
                _storageMods.Add(storageMod);
                foreach (var repoMod in _repoMods)
                {
                    CheckMatch(repoMod, storageMod);
                }
            }
        }

        public void AddRepoMod(RepositoryMod repoMod)
        {
            lock (_lock)
            {
                if (_repoMods.Contains(repoMod)) return;
                _repoMods.Add(repoMod);
                foreach (var storageMod in _storageMods)
                {
                    CheckMatch(repoMod, storageMod);
                }
            }
        }

        private void CheckMatch(RepositoryMod repoMod, StorageMod storageMod)
        {
            if (repoMod.MatchHash.IsMatch(storageMod.MatchHash) ||
                storageMod.UpdateTarget?.Hash == repoMod.VersionHash.GetHashString())
                repoMod.AddMatch(storageMod);
        }
    }
}
