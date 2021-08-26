using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace BSU.Core.Concurrency
{
    public static class ConfigureAwaitFalseExtension
    {
        public static ConfiguredTaskAwaitable DropContext(this Task task) => task.ConfigureAwait(false);
        public static ConfiguredTaskAwaitable<T> DropContext<T>(this Task<T> task) => task.ConfigureAwait(false);
    }
}
