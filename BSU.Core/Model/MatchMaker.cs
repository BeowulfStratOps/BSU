using System.Collections.Generic;
using System.Linq;
using NLog;

namespace BSU.Core.Model
{
    internal class MatchMaker : IMatchMaker
    {
        public void AddStorageMod(IModelStorageMod storageMod)
        {
            throw new System.NotImplementedException();
        }

        public void AddRepositoryMod(IModelRepositoryMod repoMod)
        {
            throw new System.NotImplementedException();
        }

        public void RemoveStorageMod(IModelStorageMod mod)
        {
            throw new System.NotImplementedException();
        }
    }
}
