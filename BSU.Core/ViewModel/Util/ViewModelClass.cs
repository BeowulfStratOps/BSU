using System.ComponentModel;
using System.Runtime.CompilerServices;
using BSU.Core.Annotations;

namespace BSU.Core.ViewModel.Util
{
    public abstract class ViewModelClass : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}