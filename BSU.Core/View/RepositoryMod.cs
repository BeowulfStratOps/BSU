using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using BSU.Core.Annotations;
using BSU.Core.Hashes;
using BSU.Core.Model;
using BSU.Core.Services;
using BSU.CoreCommon;

namespace BSU.Core.View
{
    public class RepositoryMod : INotifyPropertyChanged
    {
        private readonly Model.RepositoryMod _mod;
        internal ViewModel ViewModel { get; }
        public string Name { get; }
        public string DisplayName { private set; get; }
        
        public JobSlot Loading { get; }

        public ObservableCollection<Match> Matches { get; } = new ObservableCollection<Match>();

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        internal RepositoryMod(Model.RepositoryMod mod, ViewModel viewModel)
        {
            Loading = new JobSlot(mod.Loading, nameof(Loading));
            _mod = mod;
            ViewModel = viewModel;
            Name = mod.Identifier;
            mod.ActionAdded += AddAction;
            foreach (var target in mod.Actions.Keys)
            {
                AddAction(target);
            }
            mod.ActionChanged += storageTarget =>
            {
                var match = Matches.SingleOrDefault(m => m.StorageTarget.ModelStorageTarget.Equals(storageTarget));
                match?.Update(mod.Actions[storageTarget]);
            };
            mod.Loading.OnFinished += () =>
            {
                DisplayName = mod.Implementation.GetDisplayName();
                OnPropertyChanged(nameof(DisplayName));
            };
        }

        private void AddAction(Model.Actions.StorageTarget storageTarget)
        {
            var action = _mod.Actions[storageTarget];
            ViewModel.UiDo(() => Matches.Add(new Match(ViewModel.StorageTargets[storageTarget], this, action)));
        }
    }
}
