using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BSU.Core.Model;
using BSU.Core.Storage;
using BSU.Core.ViewModel.Util;

namespace BSU.Core.ViewModel
{
    public class StorageMod : ObservableBase
    {
        private string _usedBy = "Loading...";
        private readonly IModelStorageMod _modelStorageMod;
        private string _title;

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

        internal StorageMod(IModelStorageMod mod)
        {
            _modelStorageMod = mod;
            Title = mod.Identifier;
            AsyncVoidExecutor.Execute(async () => Title = await mod.GetTitle(CancellationToken.None));
        }

        internal async Task Update()
        {
            var usedBy = await _modelStorageMod.GetUsedBy(CancellationToken.None);
            var names = usedBy.Select(m => $"{m.ParentRepository.Name}/{m.Identifier}").ToList();
            UsedBy = names.Any() ? string.Join(", ", names) : null;
        }
    }
}
