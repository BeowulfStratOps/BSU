using BSU.CoreCommon.Hashes;

namespace BSU.Core.Tests.Util;

[HashClass(HashType.Match, 10)]
public class TestMatchHash : IModHash
{
    private readonly int _match;

    public TestMatchHash(int match)
    {
        _match = match;
    }

    public bool IsMatch(IModHash other) => other is TestMatchHash hash && hash._match == _match;
}
