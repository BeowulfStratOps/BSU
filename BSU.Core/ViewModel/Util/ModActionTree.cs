using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace BSU.Core.ViewModel.Util
{
    public class ModActionTree : IReadOnlyList<ModAction>, INotifyCollectionChanged
    {
        private readonly ObservableCollection<ModAction> _list = new() {new SelectDoNothing()};

        public ModActionTree()
        {
            _list.CollectionChanged += (s,e) =>
            {
                CollectionChanged?.Invoke(s, e);
            };
        }

        public IEnumerator<ModAction> GetEnumerator()
        {
            return _list.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public int Count => _list.Count;

        public ModAction this[int index] => _list[index];

        public event NotifyCollectionChangedEventHandler CollectionChanged;

        public void Update(ModAction action)
        {
            if (action is SelectStorage)
            {
                _list.Add(action);

                return;
            }

            var oldIndex = FindIndex(a => a.AsSelection == action.AsSelection);
            if (oldIndex > -1)
                _list[oldIndex] = action;
            else
            {
                var parentId = ((SelectMod) action).StorageMod.GetStorageModIdentifiers().Storage;
                var parentIndex = FindIndex(entry =>
                    entry is SelectStorage storage && storage.DownloadStorage.Identifier == parentId);
                _list.Insert(parentIndex + 1, action);
            }
        }

        private int FindIndex(Predicate<ModAction> predicate)
        {
            for (var i = 0; i < _list.Count; i++)
            {
                if (predicate(_list[i])) return i;
            }
            return -1;
        }

        public void Remove(ModAction action) => _list.Remove(action);
    }
}
