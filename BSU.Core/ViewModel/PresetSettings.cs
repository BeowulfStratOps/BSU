﻿using System.Collections.Generic;
using BSU.Core.Launch;
using BSU.Core.ViewModel.Util;

namespace BSU.Core.ViewModel;

public class GlobalSettings : ObservableBase
{
    private bool _useBsuLauncher;
    public bool UseBsuLauncher
    {
        get => _useBsuLauncher;
        set
        {
            if (_useBsuLauncher == value) return;
            _useBsuLauncher = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(UseArmaLauncher));
        }
    }

    public bool UseArmaLauncher
    {
        get => !_useBsuLauncher;
        set => UseBsuLauncher = !value;
    }

    internal GlobalSettings(Launch.GlobalSettings initial)
    {
        Profile = initial.Profile;
        BattlEye = initial.BattlEye;
        WorldEmpty = initial.WorldEmpty;
        CloseAfterLaunch = initial.CloseAfterLaunch;
        X64 = initial.X64;
        ShowScriptErrors = initial.ShowScriptErrors;
        HugePages = initial.HugePages;
        ArmaPath = initial.ArmaPath;
        UseBsuLauncher = initial.UseBsuLauncher;
    }

    public string? Profile { get; set; }
    public bool BattlEye { get; set; }
    public bool WorldEmpty { get; set; }
    public bool CloseAfterLaunch { get; set; }
    public bool X64 { get; set; }
    public bool ShowScriptErrors { get; set; }
    public bool HugePages { get; set; }
    public string? ArmaPath { get; set; }

    public IReadOnlyList<string> Profiles { get; } = ArmaData.GetProfiles();

    public IReadOnlyList<string> Allocators { get; } = ArmaData.GetAllocators();

    public Launch.GlobalSettings ToLaunchSettings()
    {
        return new Launch.GlobalSettings
        {
            Profile = Profile,
            BattlEye = BattlEye,
            WorldEmpty = WorldEmpty,
            CloseAfterLaunch = CloseAfterLaunch,
            X64 = X64,
            ShowScriptErrors = ShowScriptErrors,
            HugePages = HugePages,
            ArmaPath = ArmaPath,
            UseBsuLauncher = UseBsuLauncher
        };
    }
}
