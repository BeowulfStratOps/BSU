using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BSU.Core.Concurrency;
using BSU.Core.Model;
using BSU.Core.Model.Updating;
using BSU.Core.Services;
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

        public ModActionTree Actions { get; }

        private void SetSelectionFromView(ModAction value)
        {
            DownloadIdentifier = Mod.Identifier;
            if (DownloadIdentifier.StartsWith("@")) DownloadIdentifier = DownloadIdentifier[1..];
            Mod.SetSelection(value?.AsSelection);
        }

        internal RepositoryMod(IModelRepositoryMod mod, IModel model, IViewModelService viewModelService)
        {
            Actions = new ModActionTree(mod, model);
            Actions.SelectionChanged += () => SetSelectionFromView(Actions.Selection);
            Mod = mod;
            _model = model;
            _viewModelService = viewModelService;
            Name = mod.Identifier;
            ToggleExpand = new DelegateCommand(() => IsExpanded = !IsExpanded);

            var downloadIdentifier = mod.DownloadIdentifier;
            if (downloadIdentifier.StartsWith("@")) downloadIdentifier = downloadIdentifier[1..];

            DownloadIdentifier = downloadIdentifier;

            mod.StateChanged += _ => OnStateChanged();
            _model.AnyChange += Update;
            Update();
        }

        private void OnStateChanged()
        {
            Info = Mod.GetModInfo();
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

        private void Update()
        {
            DownloadIdentifier = Mod.DownloadIdentifier[1..];
            ErrorText = CoreCalculation.GetErrorForSelection(Mod, _model.GetRepositoryMods()) ?? "";
            Actions.Update();
        }

        internal async Task<(IModUpdate update, Progress<FileSyncStats> progress)> StartUpdate(CancellationToken cancellationToken)
        {
            var progress = UpdateProgress.Progress;
            var update = await Mod.StartUpdate(progress, cancellationToken);
            if (update == null) return default;

            return (update, progress);
        }
    }
}
