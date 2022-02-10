using System.Windows.Input;

namespace BSU.Core.Tests.ActionBased;

public static class Extensions
{
    public static void Execute(this ICommand command)
    {
        command.Execute(null);
    }
}
