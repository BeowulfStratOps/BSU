using System.Collections.Generic;

namespace BSU.Server;

/// <summary>
/// Wrapper for server config ini file
/// </summary>
public class PresetConfig
{
    public string SourcePath { get; set; } = "";
    public string DestinationPath { get; set; } = "";
    public List<string> ModList { get; set; } = new();
    public List<string> DlcIds { get; set; } = new();
    public string PresetName { get; set; } = "";
    public string ServerAddress { get; set; } = "";
    public ushort ServerPort { get; set; } = 2302;
    public string ServerFileName { get; set; } = "server.json";
}
