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
    }

    /// <summary>
    /// Single file hash. For serialization.
    /// </summary>
    public record BsoFileHash(string FileName, byte[] Hash, ulong FileSize);
}
