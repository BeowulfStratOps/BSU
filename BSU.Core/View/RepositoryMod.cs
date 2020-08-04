using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using BSU.Core.Annotations;

namespace BSU.Core.View
{
    public class RepositoryMod : INotifyPropertyChanged
    {
        internal readonly Model.RepositoryMod Mod;
        internal ViewModel ViewModel { get; }
        public string Name { get; }
        public string DisplayName { private set; get; }

        public bool IsLoading { private set; get; }

        public ObservableCollection<Match> Matches { get; } = new ObservableCollection<Match>();
        
        public ObservableCollection<DownloadAction> Downloads { get; } = new ObservableCollection<DownloadAction>();

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        internal RepositoryMod(Model.RepositoryMod mod, ViewModel viewModel)
        {
            IsLoading = mod.GetState().IsLoading;
            Mod = mod;
            ViewModel = viewModel;
            Name = mod.Identifier;
            mod.ActionAdded += AddAction;
            foreach (var target in mod.Actions.Keys)
            {
                AddAction(target);
            }

            mod.SelectionChanged += () =>
            {
                // TODO: do stuff
            };
            mod.StateChanged += () =>
            {
                DisplayName = mod.Implementation.GetDisplayName();
                OnPropertyChanged(nameof(DisplayName));
                IsLoading = mod.GetState().IsLoading;
                OnPropertyChanged(nameof(IsLoading));
            };
            foreach (var storage in viewModel.Storages)
            {
                AddStorage(storage.ModelStorage);
            }
        }

        private void AddAction(Model.StorageMod storageMod)
        {
            var action = Mod.Actions[storageMod];
            ViewModel.UiDo(() => Matches.Add(new Match(storageMod, this, action)));
        }
        
        internal void AddStorage(Model.Storage storage)
        {
            if (storage.Implementation.CanWrite())
                ViewModel.UiDo(() => Downloads.Add(new DownloadAction(storage, this)));
        }
    }
}
