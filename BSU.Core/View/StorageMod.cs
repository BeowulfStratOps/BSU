using System.ComponentModel;
using System.Runtime.CompilerServices;
using BSU.Core.Annotations;
using BSU.Core.Model;
using BSU.Core.Sync;

namespace BSU.Core.View
{
    public class StorageMod : INotifyPropertyChanged
    {
        internal IModelStorageMod ModelStorageMod { get; }
        
        public string Identifier { get; set; }
        
        public bool IsLoading { private set; get; }
        public bool IsHashing { private set; get; }
        public bool IsUpdating { private set; get; }
        
        internal StorageMod(IModelStorageMod mod, ViewModel viewModel)
        {
            var state = mod.GetState();
            IsLoading = state.State == StorageModStateEnum.Loading;
            IsHashing = state.State == StorageModStateEnum.Hashing;
            IsUpdating = state.State == StorageModStateEnum.Updating;
            mod.StateChanged += StateChanged;
            ModelStorageMod = mod;
            Identifier = mod.ToString();
        }

        private void StateChanged()
        {
            var state = ModelStorageMod.GetState();
            IsLoading = state.State == StorageModStateEnum.Loading;
            IsHashing = state.State == StorageModStateEnum.Hashing;
            IsUpdating = state.State == StorageModStateEnum.Updating;
            OnPropertyChanged(nameof(IsLoading));
            OnPropertyChanged(nameof(IsHashing));
            OnPropertyChanged(nameof(IsUpdating));
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}