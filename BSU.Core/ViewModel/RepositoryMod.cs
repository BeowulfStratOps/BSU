using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BSU.Core.Concurrency;
using BSU.Core.Model;
using BSU.Core.Model.Updating;
using BSU.Core.Sync;
using BSU.Core.ViewModel.Util;

namespace BSU.Core.ViewModel
{
    public class RepositoryMod : ObservableBase
    {
        internal readonly IModelRepositoryMod Mod;
        private readonly IModel _model;
        private readonly IViewModelService _viewModelService;

        public string Name { get; }
        private ModInfo _info = new("Loading...", "Loading...", 0);

        public ModInfo Info
        {
            get => _info;
            private set
            {
                if (_info == value) return;
                _info = value;
                OnPropertyChanged();
            }
        }

        private string _downloadIdentifier = "";

        public FileSyncProgress UpdateProgress { get; } = new();

        public ModActionTree Actions { get; } = new();

        private void SetSelectionFromView(ModAction value)
        {
            DownloadIdentifier = Mod.Identifier;
            if (DownloadIdentifier.StartsWith("@")) DownloadIdentifier = DownloadIdentifier[1..];
            Mod.SetSelection(value.AsSelection);
            AsyncVoidExecutor.Execute(_viewModelService.Update);
        }

        internal RepositoryMod(IModelRepositoryMod mod, IModel model, IViewModelService viewModelService)
        {
            Actions.SelectionChanged += () => SetSelectionFromView(Actions.Selection);
            Mod = mod;
            _model = model;
            _viewModelService = viewModelService;
            Name = mod.Identifier;
            ToggleExpand = new DelegateCommand(() => IsExpanded = !IsExpanded);

            var downloadIdentifier = mod.DownloadIdentifier;
            if (downloadIdentifier.StartsWith("@")) downloadIdentifier = downloadIdentifier[1..];

            DownloadIdentifier = downloadIdentifier;
        }

        private async Task<ModAction> UpdateAction(IModelStorageMod storageMod)
        {
            var action = await CoreCalculation.GetModAction(Mod, storageMod, CancellationToken.None);
            if (action == ModActionEnum.Unusable)
            {
                Actions.RemoveMod(storageMod);
                return Actions.Selection;
            }
            var selection = new SelectMod(storageMod, action);
            Actions.UpdateMod(selection);
            return selection;
        }

        internal void AddStorage(IModelStorage storage)
        {
            Actions.AddStorage(storage);
        }

        internal void RemoveStorage(IModelStorage storage)
        {
            Actions.RemoveStorage(storage);
        }

        public string DownloadIdentifier
        {
            get => _downloadIdentifier;
            set
            {
                if (value == _downloadIdentifier) return;
                Mod.DownloadIdentifier = "@" + value;
                _downloadIdentifier = value;
                OnPropertyChanged();
                AsyncVoidExecutor.Execute(_viewModelService.Update);
            }
        }

        private bool _isExpanded;

        public bool IsExpanded
        {
            get => _isExpanded;
            set
            {
                if (_isExpanded == value) return;
                _isExpanded = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(NotIsExpanded));
            }
        }

        public DelegateCommand ToggleExpand { get; }

        private string _errorText;

        public string ErrorText
        {
            get => _errorText;
            private set
            {
                if (value == _errorText) return;
                _errorText = value;
                OnPropertyChanged();
            }
        }

        public bool NotIsExpanded => !IsExpanded;

        public async Task Load()
        {
            Info = await Mod.GetModInfo(CancellationToken.None);
            foreach (var storage in await _model.GetStorages().WhereAsync(s => s.IsAvailable()))
            {
                AddStorage(storage);
            }
        }

        public async Task Update()
        {
            var selection = await Mod.GetSelection(cancellationToken: CancellationToken.None);
            DownloadIdentifier = Mod.DownloadIdentifier[1..];
            if (selection is RepositoryModActionStorageMod actionStorageMod)
            {
                var updatedAction = await UpdateAction(actionStorageMod.StorageMod);
                Actions.SetSelection(updatedAction);
            }
            else
            {
                var action = await ModAction.Create(selection, Mod, CancellationToken.None);
                Actions.SetSelection(action);
            }

            ErrorText = await Mod.GetErrorForSelection(CancellationToken.None) ?? "";

            var actions = await Mod.GetModActions(CancellationToken.None);
            foreach (var (mod, _) in actions)
            {
                await UpdateAction(mod);
            }

            // TODO: really, this should be the only thing happening in this method. keep it all functional/state-less.
            Actions.Update();
        }

        internal async Task<(IModUpdate update, Progress<FileSyncStats> progress)> StartUpdate(CancellationToken cancellationToken)
        {
            var progress = UpdateProgress.Progress;
            var update = await Mod.StartUpdate(progress, cancellationToken);
            if (update == null) return default;
            await _viewModelService.Update();

            return (update, progress);
        }
    }
}
