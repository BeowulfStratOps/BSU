using System.Collections.Generic;

namespace BSU.Core.State
{
    /// <summary>
    /// Base class for mod action choices.
    /// </summary>
    public abstract class ModAction
    {
        public readonly UpdateTarget UpdateTarget;
        private readonly List<ModAction> _conflicts = new List<ModAction>();

        internal void AddConflict(ModAction action) => _conflicts.Add(action);

        public IEnumerable<ModAction> GetConflicts() => _conflicts.AsReadOnly();

        protected ModAction(UpdateTarget updateTarget)
        {
            UpdateTarget = updateTarget;
        }
    }

    /// <summary>
    /// Convenience interface.
    /// </summary>
    public interface IHasStorageMod
    {
        StorageMod GetStorageMod();
    }
}
