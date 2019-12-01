using System.Linq;

namespace BSU.Hashes
{
    public interface FileHash
    {
        // TODO: use MurmurHash.
        byte[] GetBytes();
    }
}
