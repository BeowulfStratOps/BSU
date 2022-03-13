using System.Collections.Generic;

namespace BSU.Core.ViewModel.Util;

public static class Extensions
{
    public static IEnumerable<(int, T)> Enumerate<T>(this IEnumerable<T> enumerable)
    {
        var i = 0;
        foreach (var entry in enumerable)
        {
            yield return (i, entry);
            i++;
        }
    }
}
