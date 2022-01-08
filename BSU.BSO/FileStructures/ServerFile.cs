using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace BSU.BSO.FileStructures
{
    /// <summary>
    /// Server meta data. To be serialized on a server.
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    public class ServerFile
    {
        [JsonProperty] public string ServerName { get; set; } = null!;
        [JsonProperty] public string ServerAddress { get; set; } = null!;
        [JsonProperty] public ushort ServerPort { get; set; }
        [JsonProperty] public string Password { get; set; } = null!;
        [JsonProperty] public DateTime CreationDate { get; set; }
        [JsonProperty] public DateTime LastUpdateDate { get; set; }
        [JsonProperty] public List<Uri> SyncUris { get; set; } = null!;
        [JsonProperty] public Guid ServerGuid { get; set; }
        [JsonProperty] public List<ModFolder> ModFolders { get; set; } = null!;
        [JsonProperty] public List<string> DLCs { get; set; } = null!;

        public ServerFile(string serverName, string serverAddress, ushort serverPort, string password,
            List<ModFolder> modFolders, DateTime lastUpdate, DateTime creationDate, Guid serverGuid, List<Uri> syncUris, List<string> dlcs)
        {
            ServerAddress = serverAddress;
            ServerName = serverName;
            Password = password;
            ServerPort = serverPort;
            ModFolders = modFolders;
            CreationDate = creationDate;
            ServerGuid = serverGuid;
            SyncUris = syncUris;
            LastUpdateDate = lastUpdate;
            DLCs = dlcs;
        }

        public ServerFile()
        {
        }
    }

    public class ModFolder
    {
        public string ModName { get; set; } = null!;

        public ModFolder(string modName)
        {
            ModName = modName;
        }

        public ModFolder()
        {
        }
    }
}
