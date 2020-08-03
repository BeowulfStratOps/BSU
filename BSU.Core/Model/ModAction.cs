using System;
using System.Collections.Generic;

namespace BSU.Core.Model
{
    internal class ModAction
    {
        private readonly StorageMod _target;
        internal readonly RepositoryMod Parent;
        public ModActionEnum ActionType { get; private set; }

        public HashSet<ModAction> Conflicts = new HashSet<ModAction>();

        public ModAction(StorageMod target, ModActionEnum newAction, RepositoryMod parent)
        {
            _target = target;
            ActionType = newAction;
            Parent = parent;
            foreach (var otherModAction in target.RelatedModActions)
            {
                var isConflict = IsConflict(this, otherModAction);
                UpdateConflict(otherModAction, isConflict);
                otherModAction.UpdateConflict(this, isConflict);
            }
            target.RelatedModActions.Add(this);
        }

        public void Remove()
        {
            // TODO: ensure method gets called!
            _target.RelatedModActions.Add(this);
            foreach (var otherModAction in _target.RelatedModActions)
            {
                otherModAction.UpdateConflict(this, false);
            }
        }
        
        public void UpdateConflict(ModAction modAction, bool isConflict)
        {
            if (isConflict)
            {
                if (!Conflicts.Add(modAction)) return;
                ConflictAdded?.Invoke(modAction);
            }
            else
            {
                if (!Conflicts.Remove(modAction)) return;
                ConflictRemoved?.Invoke(modAction);
            }
        }

        public void Update(ModActionEnum newAction)
        {
            ActionType = newAction;
            Updated?.Invoke();
        }

        private bool IsConflict(ModAction a, ModAction b)
        {
            var ap = a.Parent;
            var bp = b.Parent;
            return !ap.GetState().VersionHash.IsMatch(bp.GetState().VersionHash);
        }

        public event Action Updated;
        public event Action<ModAction> ConflictAdded, ConflictRemoved;
    }
}