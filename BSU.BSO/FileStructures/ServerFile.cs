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
        [JsonProperty] public string ServerName { get; set; }
        [JsonProperty] public string ServerAddress { get; set; }
        [JsonProperty] public int ServerPort { get; set; }
        [JsonProperty] public string Password { get; set; }
        [JsonProperty] public DateTime CreationDate { get; set; }
        [JsonProperty] public DateTime LastUpdateDate { get; set; }
        [JsonProperty] public List<Uri> SyncUris { get; set; }
        [JsonProperty] public Guid ServerGuid { get; set; }
        [JsonProperty] public List<ModFolder> ModFolders { get; set; }

        public ServerFile(string serverName, string serverAddress, int servePort, string password,
            List<ModFolder> modFolders, DateTime lastUpdate, DateTime creationDate, Guid serverGuid, List<Uri> syncUris)
        {
            ServerAddress = serverAddress;
            ServerName = serverName;
            Password = password;
            ServerPort = ServerPort;
            ModFolders = modFolders;
            CreationDate = creationDate;
            ServerGuid = serverGuid;
            SyncUris = syncUris;
            LastUpdateDate = lastUpdate;
        }

        public ServerFile()
        {
        }
    }

    public class ModFolder
    {
        public string ModName { get; set; }

        public ModFolder(string modName)
        {
            ModName = modName;
        }

        public ModFolder()
        {
        }
    }
}
