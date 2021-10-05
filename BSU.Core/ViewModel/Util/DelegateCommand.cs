using System;
using System.Windows.Input;

namespace BSU.Core.ViewModel.Util
{
    public class DelegateCommand : ICommand
    {
        private readonly Action _action;
        private readonly Action<object> _objAction;
        private bool _canExecute;

        public DelegateCommand(Action action, bool canExecute = true)
        {
            _action = action;
            _canExecute = canExecute;
        }

        public DelegateCommand(Action<object> action, bool canExecute = true)
        {
            _objAction = action;
            _canExecute = canExecute;
        }

        internal void SetCanExecute(bool value)
        {
            if (value == _canExecute) return;
            _canExecute = value;
            CanExecuteChanged?.Invoke(null, EventArgs.Empty);
        }

        public bool CanExecute(object parameter) => _canExecute;
        public void Execute(object parameter)
        {
            if (_objAction != null)
                _objAction(parameter);
            else
                _action();
        }

        public event EventHandler CanExecuteChanged;
    }
}
