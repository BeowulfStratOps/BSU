namespace BSU.CoreCommon.Hashes;

public interface IModHash
{
    bool IsMatch(IModHash other);
}
