using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace BSU.Core.Launch.BiFileTypes;

public class Local
{
    [JsonProperty("autodetectionDirectories")]
    public List<string> AutodetectionDirectories { get; set; } = null!;

    [JsonProperty("dateCreated")]
    public DateTime DateCreated { get; set; }

    [JsonProperty("knownLocalMods")]
    public List<string?> KnownLocalMods { get; set; } = null!;

    [JsonProperty("userDirectories")]
    public List<string?> UserDirectories { get; set; } = null!;
}
