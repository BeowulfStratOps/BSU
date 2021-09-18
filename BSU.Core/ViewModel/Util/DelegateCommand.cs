using System;
using System.Threading.Tasks;
using System.Windows.Input;

namespace BSU.Core.ViewModel.Util
{
    public class DelegateCommand : ICommand
    {
        private readonly Action _action;
        private bool _canExecute;

        public DelegateCommand(Func<Task> action, bool canExecute = true)
        {
            // TODO: async void = bad
            _action = async () =>
            {
                await action();
            };
            _canExecute = canExecute;
        }

        public DelegateCommand(Action action, bool canExecute = true)
        {
            _action = action;
            _canExecute = canExecute;
        }

        internal void SetCanExecute(bool value)
        {
            if (value == _canExecute) return;
            _canExecute = value;
            CanExecuteChanged?.Invoke(null, EventArgs.Empty);
        }

        public bool CanExecute(object parameter) => _canExecute;
        public void Execute(object parameter) => _action();

        public event EventHandler CanExecuteChanged;
    }
}
