using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using BSU.Core.Annotations;
using BSU.Core.Model;
using BSU.CoreCommon;

namespace BSU.Core.View
{
    public class Storage : INotifyPropertyChanged
    {
        private readonly ViewModel _viewModel;
        public string Name { get; }
        internal Model.Storage ModelStorage { get; }
        
        public bool IsLoading { get; }

        public ObservableCollection<StorageMod> Mods { get; } = new ObservableCollection<StorageMod>();

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        internal Storage(Model.Storage storage, ViewModel viewModel)
        {
            IsLoading = storage.Loading.IsActive();
            ModelStorage = storage;
            _viewModel = viewModel;
            Name = storage.Identifier;
            storage.ModAdded += mod => ViewModel.UiDo(() => Mods.Add(new StorageMod(mod, viewModel)));
        }
    }
}
