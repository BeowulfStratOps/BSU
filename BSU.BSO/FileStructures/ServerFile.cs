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
        [JsonProperty] public DateTime LastUpdateDate { get; set; }
        [JsonProperty] public List<Uri> SyncUris { get; set; } = null!;
        [JsonProperty] public List<ModFolder> ModFolders { get; set; } = null!;

        [JsonProperty] public List<string>? Dlcs { get; set; }
    }

    public record ModFolder(string ModName);
}
