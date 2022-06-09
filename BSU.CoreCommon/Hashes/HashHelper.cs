using System;
using System.Reflection;

namespace BSU.CoreCommon.Hashes;

public static class HashHelper
{
    private static HashClassAttribute GetAttribute(Type type) =>
        type.GetCustomAttribute<HashClassAttribute>() ?? throw new InvalidOperationException($"{type} has no HashClass Attribute");
    
    public static HashType GetHashType(Type type) => GetAttribute(type).HashType;

    public static int GetPriority(Type type) => GetAttribute(type).Priority;
}
