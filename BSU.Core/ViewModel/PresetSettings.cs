using System;
using System.Collections.Generic;
using BSU.Core.Launch;
using BSU.Core.ViewModel.Util;

namespace BSU.Core.ViewModel;

public class PresetSettings : ObservableBase
{

    internal PresetSettings(Launch.PresetSettings initial)
    {
        Profile = initial.Profile;
        Server = initial.Server;
        BattlEye = initial.BattlEye;
        WorldEmpty = initial.WorldEmpty;
        CloseAfterLaunch = initial.CloseAfterLaunch;
        X64 = initial.X64;
        ShowScriptErrors = initial.ShowScriptErrors;
        HugePages = initial.HugePages;
        Allocator = initial.Allocator;
        ArmaPath = initial.ArmaPath;
    }

    public string Profile { get; set; }
    public string? Server { get; set; }
    public bool BattlEye { get; set; }
    public bool WorldEmpty { get; set; }
    public bool CloseAfterLaunch { get; set; }
    public bool X64 { get; set; }
    public bool ShowScriptErrors { get; set; }
    public bool HugePages { get; set; }
    public string Allocator { get; set; }
    public string? ArmaPath { get; set; }

    public IReadOnlyList<string> Profiles { get; } = ArmaData.GetProfiles();

    public IReadOnlyList<string> Allocators { get; } = ArmaData.GetAllocators();

    public Launch.PresetSettings ToLaunchSettings()
    {
        return new Launch.PresetSettings(Profile, Allocator)
        {
            Server = Server,
            BattlEye = BattlEye,
            WorldEmpty = WorldEmpty,
            CloseAfterLaunch = CloseAfterLaunch,
            X64 = X64,
            ShowScriptErrors = ShowScriptErrors,
            HugePages = HugePages,
            ArmaPath = ArmaPath
        };
    }
}
