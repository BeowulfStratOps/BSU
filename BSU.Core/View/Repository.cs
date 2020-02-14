using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using BSU.Core.Annotations;
using BSU.Core.Model;
using BSU.Core.Services;
using BSU.CoreCommon;

namespace BSU.Core.View
{
    public class Repository : INotifyPropertyChanged
    {
        private readonly ViewModel _viewModel;
        public string Name { get; }

        public JobSlot Loading { get; }
        
        public ObservableCollection<RepositoryMod> Mods { get; } = new ObservableCollection<RepositoryMod>();

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        internal Repository(Model.Repository repository, ViewModel viewModel)
        {
            Loading = new JobSlot(repository.Loading, nameof(Loading));
            _viewModel = viewModel;
            Name = repository.Identifier;
            repository.ModAdded += mod => ViewModel.UiDo(() => Mods.Add(new RepositoryMod(mod, viewModel)));
        }
    }
}
