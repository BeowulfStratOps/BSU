﻿using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BSU.Core.Model;
using BSU.Core.Services;
using BSU.Core.Storage;
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

        internal StorageMod(IModelStorageMod mod, IModel model)
        {
            _modelStorageMod = mod;
            _model = model;
            _title = mod.Identifier;

            mod.StateChanged += _ => OnStateChanged();
            _model.AnyChange += Update;
        }

        private void OnStateChanged()
        {
            Title = _modelStorageMod.GetTitle();
        }

        private void Update()
        {
            var usedBy = CoreCalculation.GetUsedBy(_modelStorageMod, _model.GetRepositoryMods());
            var names = usedBy.Select(m => $"{m.ParentRepository.Name}/{m.Identifier}").ToList();
            UsedBy = names.Any() ? string.Join(", ", names) : null;
        }
    }
}
