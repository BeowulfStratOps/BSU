using System;
using System.Collections.Generic;
using BSU.Core.Hashes;

namespace BSU.Core.Model
{
    internal class ModAction
    {
        internal readonly IModelRepositoryMod Parent;
        private readonly VersionHash _versionHash;
        private readonly HashSet<ModAction> _relatedActions;
        public ModActionEnum ActionType { get; private set; }

        public readonly HashSet<ModAction> Conflicts = new HashSet<ModAction>();

        public ModAction(ModActionEnum newAction, IModelRepositoryMod parent, VersionHash versionHash, HashSet<ModAction> relatedActions)
        {
            ActionType = newAction;
            Parent = parent;
            _versionHash = versionHash;
            _relatedActions = relatedActions;
            foreach (var otherModAction in relatedActions)
            {
                var isConflict = IsConflict(otherModAction);
                UpdateConflict(otherModAction, isConflict);
                otherModAction.UpdateConflict(this, isConflict);
            }
            relatedActions.Add(this);
        }

        public void Remove()
        {
            // TODO: ensure method gets called!
            _relatedActions.Remove(this);
            foreach (var otherModAction in _relatedActions)
            {
                otherModAction.UpdateConflict(this, false);
            }
        }
        
        private void UpdateConflict(ModAction modAction, bool isConflict)
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

        private bool IsConflict(ModAction other)
        {
            return !_versionHash.IsMatch(other._versionHash);
        }

        public event Action Updated;
        public event Action<ModAction> ConflictAdded, ConflictRemoved;
    }
}