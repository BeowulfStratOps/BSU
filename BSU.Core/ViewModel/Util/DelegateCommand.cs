using System;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using NLog;

namespace BSU.Core.ViewModel.Util
{
    public class DelegateCommand : ICommand
    {
        private readonly Action? _action;
        private readonly Action<object?>? _objAction;
        private bool _canExecute;
        private readonly string? _actionCode;
        private readonly ILogger _logger = LogManager.GetCurrentClassLogger();

        // TODO: create AsyncDelegateCommand
        public DelegateCommand(Action action, bool canExecute = true, [CallerArgumentExpression("action")] string? actionCode = null)
        {
            _action = action;
            _canExecute = canExecute;
            _actionCode = actionCode;
        }

        public DelegateCommand(Action<object?> action, bool canExecute = true, [CallerArgumentExpression("action")] string? actionCode = null)
        {
            _objAction = action;
            _canExecute = canExecute;
            _actionCode = actionCode;
        }

        internal void SetCanExecute(bool value)
        {
            if (value == _canExecute) return;
            _canExecute = value;
            CanExecuteChanged?.Invoke(null, EventArgs.Empty);
        }

        public bool CanExecute(object? parameter) => _canExecute;
        public void Execute(object? parameter)
        {
            try
            {
                if (!_canExecute) throw new InvalidOperationException();
                _logger.Trace($"Executing command: {_actionCode}");
                if (_objAction != null)
                    _objAction(parameter);
                else
                    _action!();
            }
            catch (Exception e)
            {
                _logger.Error(e);
                throw;
            }
        }

        public event EventHandler? CanExecuteChanged;
    }
}
