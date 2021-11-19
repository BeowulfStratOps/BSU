using System.Linq;
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
        private string _usedBy = "Loading...";
        private readonly IModelStorageMod _modelStorageMod;
        private string _title;
        private readonly Helper _helper;

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

        public string UsedBy
        {
            get => _usedBy;
            set
            {
                if (_usedBy == value) return;
                _usedBy = value;
                OnPropertyChanged();
            }
        }

        internal StorageMod(IModelStorageMod mod, Helper helper)
        {
            _modelStorageMod = mod;
            _helper = helper;
            Title = mod.Identifier;

            mod.StateChanged += _ => OnStateChanged();
            _helper.AnyChange += Update;
        }

        private void OnStateChanged()
        {
            Title = _modelStorageMod.GetTitle();
        }

        private void Update()
        {
            var usedBy = _helper.GetUsedBy(_modelStorageMod);
            var names = usedBy.Select(m => $"{m.ParentRepository.Name}/{m.Identifier}").ToList();
            UsedBy = names.Any() ? string.Join(", ", names) : null;
        }
    }
}
