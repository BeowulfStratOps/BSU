using System;
using System.Linq;
using BSU.Core.Events;
using BSU.Core.Ioc;
using BSU.Core.Model;
using BSU.Core.Services;
using BSU.Core.ViewModel.Util;

namespace BSU.Core.ViewModel
{
    public class StorageMod : ObservableBase
    {
        private string? _usedBy = "Loading...";
        private readonly IModelStorageMod _modelStorageMod;
        private string _title;
        private readonly IModel _model;

        public string Title
        {
            get => _title;
            set
            {
                if (_title == value) return;
                _title = value;
                OnPropertyChanged();
            }
        }

        public string? UsedBy
        {
            get => _usedBy;
            set
            {
                if (_usedBy == value) return;
                _usedBy = value;
                OnPropertyChanged();
            }
        }

        internal StorageMod(IModelStorageMod mod, IServiceProvider serviceProvider)
        {
            _modelStorageMod = mod;
            _model = serviceProvider.Get<IModel>();
            _title = mod.Identifier;

            mod.StateChanged += OnStateChanged;
            serviceProvider.Get<IEventManager>().Subscribe<AnythingChangedEvent>(_ => Update());
        }

        private void OnStateChanged()
        {
            Title = _modelStorageMod.GetTitle();
        }

        private void Update()
        {
            var usedBy = CoreCalculation.GetUsedBy(_modelStorageMod, _model.GetRepositoryMods());
            var names = usedBy.Select(m => $"{m.ParentRepository.Name}").ToList();
            UsedBy = names.Any() ? string.Join(", ", names) : null;
        }
    }
}
