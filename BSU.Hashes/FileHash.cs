using System;
using System.Linq;

namespace BSU.Hashes
{
    public abstract class FileHash
    {
        // TODO: replace SHA1 with MurmurHash?
        public abstract byte[] GetBytes();
        public abstract long GetFileLength();

        public override bool Equals(object obj)
        {
            if (!(obj is FileHash otherHash)) return false;
            return GetBytes().SequenceEqual(otherHash.GetBytes());
        }

        public override int GetHashCode() => GetBytes().Aggregate(1, (current, t) => current * t % 1234354566);
    }
}
