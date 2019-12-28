using System.Linq;

namespace BSU.Hashes
{
    /// <summary>
    /// Base class for file hashes used in BSU.
    /// </summary>
    public abstract class FileHash
    {
        // TODO: replace SHA1 with MurmurHash?
        public abstract byte[] GetBytes();

        public override bool Equals(object obj)
        {
            if (!(obj is FileHash otherHash)) return false;
            return GetBytes().SequenceEqual(otherHash.GetBytes());
        }

        /// <summary>
        /// Accompanies equals method
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode() => GetBytes().Aggregate(1, (current, t) => current * t % 1234354566); // TODO: check if that makes any sense..
    }
}
