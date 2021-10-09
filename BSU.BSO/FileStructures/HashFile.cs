using System.Collections.Generic;
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
        [JsonProperty] public string FolderName { get; set; }
        [JsonProperty] public List<HashType> Hashes { get; set; }

        public HashFile()
        {
        }

        public HashFile(string folderName, List<HashType> hashes)
        {
            Hashes = hashes;
            FolderName = folderName;
        }
    }

    /// <summary>
    /// Single file hash. For serialization.
    /// </summary>
    public class HashType
    {
        public string FileName { get; set; }

        /// <summary>
        /// Pbo / SHA1 hash
        /// </summary>
        public byte[] Hash { get; set; }
        public ulong FileSize { get; set; }

        public HashType(string fileName, byte[] hash, ulong fileSize)
        {
            FileName = fileName;
            Hash = hash;
            FileSize = fileSize;
        }

        public override string ToString()
        {
            return "Hash: " + FileName;
        }
    }
}
