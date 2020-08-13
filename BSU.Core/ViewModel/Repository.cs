using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using BSU.Core.Annotations;
using BSU.Core.Model;

namespace BSU.Core.ViewModel
{
    public class Repository : ViewModelClass
    {
        public string Name { get; }

        public string CalculatedState { get; private set; }
        
        public ObservableCollection<RepositoryMod> Mods { get; } = new ObservableCollection<RepositoryMod>();

        internal Repository(Model.Repository repository, ViewModel viewModel, IModelStructure structure)
        {
            CalculatedState = repository.CalculatedState.ToString();
            repository.CalculatedStateChanged += () =>
            {
                CalculatedState = repository.CalculatedState.ToString();
                OnPropertyChanged(nameof(CalculatedState));
            };
            Name = repository.Identifier;
            repository.ModAdded += mod => Mods.Add(new RepositoryMod(mod, structure));
        }
    }
}
