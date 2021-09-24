using System;
using System.Threading.Tasks;
using System.Windows.Input;

namespace BSU.Core.ViewModel.Util
{
    public interface IStateCommand
    {
        Action Execute { get; }
        CommandState State { get; }
        event Action StateChanged;
    }

    public enum CommandState
    {
        Loading,
        Disabled,
        Warning,
        Enabled,
        Primary
    }

    public class StateCommand : IStateCommand
    {
        private CommandState _state;

        public Action Execute { get; }

        public CommandState State
        {
            get => _state;
            set
            {
                if (_state == value) return;
                _state = value;
                StateChanged?.Invoke();
            }
        }

        public event Action StateChanged;

        public StateCommand(Action execute, CommandState initialState = CommandState.Enabled)
        {
            Execute = execute;
            _state = initialState;
        }

        public StateCommand(Func<Task> execute, CommandState initialState = CommandState.Enabled)
        {
            // TODO: await _somewhere_
            Execute = async () =>
            {
                await execute();
            };
            _state = initialState;
        }
    }
}
