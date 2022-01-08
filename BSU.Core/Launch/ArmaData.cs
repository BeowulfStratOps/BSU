using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Win32;
using NLog;

namespace BSU.Core.Launch;

public static class ArmaData
{
    public static List<string> GetProfiles()
    {
        return GetProfilesFrom("Arma 3").Concat(GetProfilesFrom("Arma 3 - Other Profiles")).ToList();
    }

    private static HashSet<string> GetProfilesFrom(string dirName)
    {
        var profiles = new HashSet<string>();
        var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), dirName);
        var dirInfo = new DirectoryInfo(path);
        if (!dirInfo.Exists) return profiles;
        foreach (var file in dirInfo.EnumerateFiles("*.Arma3Profile", SearchOption.AllDirectories))
        {
            var name = file.Name.Split(".")[0];
            profiles.Add(name);
        }

        return profiles;
    }

    public static string? GetGamePath()
    {
        // TODO: fails if the game was never started. Could check steam libraries in that case
        var localKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
        var localValue = localKey.OpenSubKey(@"SOFTWARE\Bohemia Interactive\ArmA 3")?.GetValue("main");
        if (localValue != null)
        {
            return localValue.ToString();
        }

        var localkey32 = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32);
        var localValue32 = localkey32.OpenSubKey(@"SOFTWARE\Bohemia Interactive\ArmA 3")?.GetValue("main");
        if (localValue32 != null)
        {
            return localValue32.ToString();
        }

        LogManager.GetCurrentClassLogger().Error("Couldn't find arma install path");
        return null;
    }

    public static bool Is64BitSystem() => Environment.Is64BitOperatingSystem;

    // ReSharper disable once InconsistentNaming
    public static List<(ulong id, string path)> GetInstalledCDLCs()
    {
        var armaPath = GetGamePath();

        if (armaPath == null) return new List<(ulong id, string path)>();

        return new List<(ulong id, string path)>
        {
            (1681170U, "WS"),
            (1227700U, "VN"),
            (1294440U, "CLSA"),
            (1042220U, "GM")
        }.Where((dlc, _) => new DirectoryInfo(Path.Combine(armaPath, dlc.path)).Exists).ToList();
    }

    public static IReadOnlyList<string> GetAllocators()
    {
        return new List<string> { "System" };
    }
}
