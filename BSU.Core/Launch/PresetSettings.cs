using System.Linq;

namespace BSU.Core.Launch;

public record PresetSettings(string Allocator)
{
    public string? Profile { get; init; }
    public bool BattlEye { get; init; }
    public bool WorldEmpty { get; init; }
    public bool CloseAfterLaunch { get; init; }

    public bool X64 { get; init; }
    public bool ShowScriptErrors { get; init; }
    public bool HugePages { get; init; }
    public string? ArmaPath { get; init; }
    public bool UseBsuLauncher { get; init; }

    public static PresetSettings BuildDefault()
    {
        return new PresetSettings(ArmaData.GetAllocators().First())
        {
            Profile = ArmaData.GetProfiles().FirstOrDefault(),
            BattlEye = true,
            WorldEmpty = true,
            HugePages = true,
            X64 = ArmaData.Is64BitSystem(),
            ArmaPath = ArmaData.GetGamePath(),
            UseBsuLauncher = true
        };
    }
}
