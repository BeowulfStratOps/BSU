using System;

namespace BSU.CoreCommon.Hashes;

[AttributeUsage(AttributeTargets.Class)]
public class HashClassAttribute : Attribute
{
    public HashType HashType { get; }
    public int Priority { get; }

    public HashClassAttribute(HashType hashType, int priority)
    {
        HashType = hashType;
        Priority = priority;
    }
}
