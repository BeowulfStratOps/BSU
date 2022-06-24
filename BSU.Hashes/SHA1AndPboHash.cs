using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace BSU.Hashes
{
    /// <summary>
    /// Fast but accurate hash for pbo files.
    /// Reads the builtin hash for pbo files, calculates SHA1 for other files.
    /// </summary>
    public class Sha1AndPboHash : FileHash
    {
        private readonly byte[] _hash;

        /// <summary>
        /// Calculates hash from file stream. Closes the stream.
        /// </summary>
        /// <param name="file"></param>
        /// <param name="extension"></param>
        /// <param name="cancellationToken"></param>
        public static async Task<Sha1AndPboHash> BuildAsync(Stream file, string extension, CancellationToken cancellationToken)
        {
            await using (file)
            {
                if ((extension == "pbo" || extension == "ebo") && file.Length > 20 && file.CanSeek)
                {
                    var storedHash = new byte[20];
                    file.Seek(-20L, SeekOrigin.End);
                    var read = await file.ReadAsync(storedHash, cancellationToken);
                    if (read != storedHash.Length)
                        throw new NotImplementedException();
                    return new Sha1AndPboHash(storedHash);
                }

                using var sha1 = SHA1.Create();
                var hash = await sha1.ComputeHashAsync(file, cancellationToken);
                return new Sha1AndPboHash(hash);
            }
        }

        /// <summary>
        /// Instantiates from known hash.
        /// </summary>
        /// <param name="hash"></param>
        public Sha1AndPboHash(byte[] hash)
        {
            _hash = hash;
        }

        public override byte[] GetBytes() => _hash;
    }
}
