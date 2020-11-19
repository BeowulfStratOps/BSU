using System;
using System.Threading.Tasks;
using System.Windows.Input;

namespace BSU.Core.ViewModel.Util
{
    public class DelegateCommand : ICommand
    {
        private readonly Func<Task> _action;
        private bool _canExecute;

        public DelegateCommand(Func<Task> action, bool canExecute = true)
        {
            _action = action;
            _canExecute = canExecute;
        }

        internal void SetCanExecute(bool value)
        {
            if (value == _canExecute) return;
            _canExecute = value;
            CanExecuteChanged?.Invoke(null, new EventArgs());
        }

        public bool CanExecute(object parameter) => _canExecute;

        // TODO: async void = bad
        public async void Execute(object parameter) => await _action();

        public event EventHandler CanExecuteChanged;
    }
}