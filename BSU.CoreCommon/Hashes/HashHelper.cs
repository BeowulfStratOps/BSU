using System;
using System.Linq;
using System.Reflection;

namespace BSU.CoreCommon.Hashes;

public static class HashHelper
{
    private static HashClassAttribute GetAttribute(Type type) =>
        type.GetCustomAttribute<HashClassAttribute>() ?? throw new InvalidOperationException($"{type} has no HashClass Attribute");

    private static HashType GetHashType(Type type) => GetAttribute(type).HashType;

    private static int GetPriority(Type type) => GetAttribute(type).Priority;

    public static bool? CheckHash(HashType type, IHashCollection mod1, IHashCollection mod2)
    {
        var supportedTypes1 = mod1.GetSupportedHashTypes();
        var supportedTypes2 = mod2.GetSupportedHashTypes();
        var supportedType = supportedTypes1.Intersect(supportedTypes2).Where(t => GetHashType(t) == type)
            .MaxBy(GetPriority);

        if (supportedType == null)
            throw new NotSupportedException($"Could not find a common hash of type {type} for {mod1} and {mod2}. Candidates were {string.Join(", ", supportedTypes1)} and {string.Join(", ", supportedTypes2)}.");

        var hashTask1 = mod1.GetHash(supportedType);
        var hashTask2 = mod2.GetHash(supportedType);
            
        if (!hashTask1.IsCompleted || !hashTask2.IsCompleted) return null;

        var hash1 = hashTask1.GetAwaiter().GetResult();
        var hash2 = hashTask2.GetAwaiter().GetResult();

        return hash1.IsMatch(hash2);
    }
}
