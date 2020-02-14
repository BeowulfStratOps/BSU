using System.Collections.Generic;

namespace BSU.Core.Model.Actions
{
    /// <summary>
    /// Base class for mod action choices.
    /// </summary>
    internal abstract class ModAction
    {
        public UpdateTarget UpdateTarget { get; }
        public readonly List<ModAction> Conflicts = new List<ModAction>(); // TODO: use

        protected ModAction(UpdateTarget updateTarget)
        {
            UpdateTarget = updateTarget;
        }
    }
}
