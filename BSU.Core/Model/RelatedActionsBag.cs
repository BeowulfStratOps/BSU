using System.Collections.Generic;

namespace BSU.Core.Model
{
    internal class RelatedActionsBag
    {
        private readonly Dictionary<IModelStorageMod, HashSet<ModAction>> _bag =
            new Dictionary<IModelStorageMod, HashSet<ModAction>>();

        public HashSet<ModAction> GetBag(IModelStorageMod storageMod)
        {
            if (_bag.TryGetValue(storageMod, out var bag)) return bag;
            var newBag = new HashSet<ModAction>();
            _bag[storageMod] = newBag;
            return newBag;
        }
    }
}