using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using BSU.Core.Annotations;
using BSU.Core.Model;

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

        public object Selection { get; private set; }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void SetSelection(Model.RepositoryMod mod)
        {
            // Use setter for this
            if (mod.SelectedStorageMod != null)
            {
                Selection = mod.SelectedStorageMod.ToString();
                OnPropertyChanged(nameof(Selection));
                return;
            }
            if (mod.SelectedDownloadStorage != null)
            {
                Selection = mod.SelectedDownloadStorage.Identifier;
                OnPropertyChanged(nameof(Selection));
                return;
            }

            Selection = "None";
            OnPropertyChanged(nameof(Selection));
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
            
            SetSelection(mod);
            mod.SelectionChanged += () => SetSelection(mod);
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

        private void AddAction(IModelStorageMod storageMod)
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
