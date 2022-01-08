using System.Linq;

namespace BSU.Core.Launch;

public record LaunchSettings(string Profile, string Allocator)
{
    public string? Server { get; init; }
    public bool BattlEye { get; init; }
    public bool WorldEmpty { get; init; }
    public bool CloseAfterLaunch { get; init; }

    public bool X64 { get; init; }
    public bool ShowScriptErrors { get; init; }
    public bool HugePages { get; init; }
    public string? ArmaPath { get; init; }

    public static LaunchSettings BuildDefault()
    {
        return new LaunchSettings(ArmaData.GetProfiles().First(), ArmaData.GetAllocators().First())
        {
            BattlEye = true,
            WorldEmpty = true,
            HugePages = true,
            X64 = ArmaData.Is64BitSystem(),
            ArmaPath = ArmaData.GetGamePath()
        };
    }
}
