using System.IO;
using System.Security.Cryptography;

namespace BSU.Hashes
{
    public class SHA1AndPboHash : FileHash
    {
        private readonly byte[] _hash;
        private readonly long _length;

        public SHA1AndPboHash(Stream file, string extension)
        {
            using (file)
            {
                if ((extension == "pbo" || extension == "ebo") && file.Length > 20 && file.CanSeek)
                {
                    _hash = new byte[20];
                    file.Seek(-20L, SeekOrigin.End);
                    file.Read(_hash, 0, 20);
                    return;
                }

                using var sha1 = SHA1.Create();
                _hash = sha1.ComputeHash(file);
                _length = file.Length;
            }
        }

        public SHA1AndPboHash(byte[] hash, long length)
        {
            _hash = hash;
            _length = length;
        }

        public override byte[] GetBytes() => _hash;

        public virtual long GetFileLength() => _length;
    }
}
