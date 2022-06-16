using BSU.CoreCommon.Hashes;

namespace BSU.Core.Tests.Util;

[HashClass(HashType.Version, 10)]
public class TestVersionHash : IModHash
{
    private readonly int _version;

    public TestVersionHash(int version)
    {
        _version = version;
    }

    public bool IsMatch(IModHash other) => other is TestVersionHash hash && hash._version == _version;
}
