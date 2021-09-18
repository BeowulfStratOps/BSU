using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace BSU.Core.ViewModel.Util
{
    public class ModActionTree : ObservableCollection<ModAction>
    {
        public ModActionTree()
        {
            Add(new SelectDoNothing());
        }

        public void Update(ModAction action)
        {
            if (action is SelectStorage)
            {
                Add(action);
                return;
            }

            var oldIndex = FindIndex(a => a.AsSelection.Equals(action.AsSelection));
            if (oldIndex > -1)
            {
                var oldAction = this[oldIndex];
                if (!oldAction.Equals(action))
                    this[oldIndex] = action;
            }
            else
            {
                var parentId = ((SelectMod) action).StorageMod.GetStorageModIdentifiers().Storage;
                var parentIndex = FindIndex(entry =>
                    entry is SelectStorage storage && storage.DownloadStorage.Identifier == parentId);
                this.Insert(parentIndex + 1, action);
            }
        }

        private int FindIndex(Predicate<ModAction> predicate)
        {
            for (var i = 0; i < Count; i++)
            {
                if (predicate(this[i])) return i;
            }
            return -1;
        }
    }
}
