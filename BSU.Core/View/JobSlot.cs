using System.ComponentModel;
using System.Runtime.CompilerServices;
using BSU.Core.Annotations;
using BSU.Core.Model;

namespace BSU.Core.View
{
    public class JobSlot : INotifyPropertyChanged
    {
        private bool _active;

        private string _name;

        internal JobSlot(IJobSlot slot, string name)
        {
            _active = slot.IsActive();
            slot.OnStarted += () =>
            {
                _active = true;
                OnPropertyChanged(nameof(Display));
            };
            slot.OnFinished += () =>
            {
                _active = false;
                OnPropertyChanged(nameof(Display));
            };
            _name = name;
        }

        public string Display => _active ? _name : "";

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}