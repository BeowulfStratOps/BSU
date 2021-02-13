using System.Collections.Generic;

namespace BSU.Core.Model
{
    internal class RelatedActionsBag
    {
        private readonly Dictionary<object, HashSet<ModAction>> _bag =
            new Dictionary<object, HashSet<ModAction>>();

        public HashSet<ModAction> GetBag(object key)
        {
            if (_bag.TryGetValue(key, out var bag)) return bag;
            var newBag = new HashSet<ModAction>();
            _bag[key] = newBag;
            return newBag;
        }
    }
}