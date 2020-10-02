using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using BSU.Core.Model;

namespace BSU.Core.ViewModel.Util
{
    public class ModActionTree : IReadOnlyList<ModAction>, INotifyCollectionChanged, INotifyPropertyChanged
    {
        private readonly List<ModAction> _list = new List<ModAction> {new SelectDoNothing()};
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
        public event PropertyChangedEventHandler PropertyChanged;


        public void Add(ModAction action)
        {
            if (action is SelectStorage)
            {
                _list.Add(action);
                return;
            }

            var parentId = ((SelectMod) action).StorageMod.GetStorageModIdentifiers().Storage;
            var parentIndex = _list.FindIndex(entry => entry is SelectStorage storage && storage.DownloadStorage.Identifier == parentId);
            _list.Insert( parentIndex + 1, action);
        }

        public void Remove(ModAction action) => _list.Remove(action);
    }
}