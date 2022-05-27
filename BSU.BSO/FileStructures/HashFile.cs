using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using BSU.Hashes;
using Newtonsoft.Json;

namespace BSU.BSO.FileStructures
{
    /// <summary>
    /// Holds Hash information for all files in a mod.
    /// To be serialized on a server.
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    public class HashFile
    {
        [JsonProperty] public string FolderName { get; set; } = null!;
        [JsonProperty] public List<BsoFileHash> Hashes { get; set; } = null!;

        public HashFile()
        {
        }

        public HashFile(string folderName, List<BsoFileHash> hashes)
        {
            Hashes = hashes;
            FolderName = folderName;
        }

        public string BuildModHash()
        {
            using var sha1 = SHA1.Create();
            var filesAndHashes = new MemoryStream();
            foreach (var hash in Hashes.OrderBy(h => h.FileName, StringComparer.InvariantCultureIgnoreCase))
            {
                filesAndHashes.Write(Encoding.UTF8.GetBytes(hash.FileName.ToLowerInvariant()));
                filesAndHashes.Write(hash.Hash);
            }
            filesAndHashes.Position = 0;
            var modHash = sha1.ComputeHash(filesAndHashes);
            return Utils.ToHexString(modHash);
        }
    }

    /// <summary>
    /// Single file hash. For serialization.
    /// </summary>
    public record BsoFileHash(string FileName, byte[] Hash, ulong FileSize);
}
