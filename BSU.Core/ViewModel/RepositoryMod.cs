using System;
using System.Threading;
using System.Threading.Tasks;
using BSU.Core.Events;
using BSU.Core.Ioc;
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

        public FileSyncProgress UpdateProgress { get; } = new();

        public ModActionTree Actions { get; }

        private void SetSelectionFromView(ModAction value)
        {
            Mod.SetSelection(value.AsSelection);
        }

        internal RepositoryMod(IModelRepositoryMod mod, IServiceProvider services, int stripeIndex)
        {
            var model = services.Get<IModel>();
            Actions = new ModActionTree(mod, model);
            Actions.SelectionChanged += () => SetSelectionFromView(Actions.Selection);
            Mod = mod;
            _stripeIndex = stripeIndex;
            _model = model;
            Name = mod.Identifier;
            ToggleExpand = new DelegateCommand(() => IsExpanded = !IsExpanded);

            mod.StateChanged += _ => OnStateChanged();
            services.Get<IEventManager>().Subscribe<AnythingChangedEvent>(_ => Update());
            Update();
        }

        private void OnStateChanged()
        {
            Info = Mod.GetModInfo();
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

        private string? _errorText;

        public string? ErrorText
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

        private int _stripeIndex;
        public int StripeIndex
        {
            get => _stripeIndex;
            set => SetProperty(ref _stripeIndex, value);
        }

        private void Update()
        {
            ErrorText = CoreCalculation.GetErrorForSelection(Mod, _model.GetRepositoryMods()) ?? "";
            Actions.Update();
        }

        internal async Task<ModUpdate?> StartUpdate(CancellationToken cancellationToken)
        {
            var progress = UpdateProgress.Progress;
            var updateInfo = await Mod.StartUpdate(progress, cancellationToken);
            return updateInfo == null ? null : new ModUpdate(updateInfo.Update, progress, updateInfo.Mod);
        }
    }

    internal record ModUpdate(Task<UpdateResult> Update, Progress<FileSyncStats> Progress, IModelStorageMod Mod);
}
