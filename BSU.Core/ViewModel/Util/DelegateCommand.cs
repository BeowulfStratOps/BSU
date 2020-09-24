using System;
using System.Windows.Input;

namespace BSU.Core.ViewModel.Util
{
    public class DelegateCommand : ICommand
    {
        private readonly Action _action;
        private bool _canExecute;

        public DelegateCommand(Action action, bool canExecute = true)
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

        public void Execute(object parameter) => _action();

        public event EventHandler CanExecuteChanged;
    }
}