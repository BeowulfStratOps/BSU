using System.Collections.Generic;
using Newtonsoft.Json;

namespace BSU.BSO.FileStructures
{
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

    public class HashType
    {
        public string FileName { get; set; }
        public byte[] Hash { get; set; }
        public long FileSize { get; set; }

        public HashType(string fileName, byte[] hash, long filesize)
        {
            FileName = fileName;
            Hash = hash;
            FileSize = filesize;
        }

        public override string ToString()
        {
            return "Hash: " + FileName;
        }
    }
}