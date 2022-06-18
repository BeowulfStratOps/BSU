using System.Windows.Input;

namespace BSU.Core.Tests.ViewModelIntegration.TestModel;

public static class Extensions
{
    public static void Execute(this ICommand command)
    {
        command.Execute(null);
    }
}
