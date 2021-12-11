using System.Collections.Generic;

namespace BSU.Core;

public static class ExtensionMethods
{
    public static void AddInBin<TKey, TValue>(this IDictionary<TKey, List<TValue>> dict, TKey key, TValue value)
    {
        if (dict.TryGetValue(key, out var existing))
        {
            existing.Add(value);
        }
        else
        {
            dict.Add(key, new List<TValue> { value });
        }
    }
}
