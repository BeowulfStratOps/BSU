using System.Collections.Generic;

namespace BSU.Core.State
{
    public abstract class ModAction
    {
        public readonly UpdateTarget UpdateTarget;
        private readonly List<ModAction> _conflicts = new List<ModAction>();

        internal void AddConflict(ModAction action) => _conflicts.Add(action);

        public IReadOnlyList<ModAction> GetConflicts() => _conflicts.AsReadOnly();

        public ModAction(UpdateTarget updateTarget)
        {
            UpdateTarget = updateTarget;
        }
    }

    public interface IHasStorageMod
    {
        StorageMod GetStorageMod();
    }
}
