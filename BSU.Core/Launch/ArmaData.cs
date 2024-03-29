﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using NLog;

namespace BSU.Core.Launch;

public static class ArmaData
{
    public static IReadOnlyDictionary<ulong, string> CDlcMap = new Dictionary<ulong, string>
    {
        { 1681170U, "WS" },
        { 1227700U, "VN" },
        { 1294440U, "CLSA" },
        { 1042220U, "GM" }
    };

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
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) throw new NotSupportedException();

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

    public static bool IsCDlcInstalled(ulong id)
    {
        var armaPath = GetGamePath();
        var cdlcPath = CDlcMap[id];
        return new DirectoryInfo(Path.Combine(armaPath!, cdlcPath)).Exists;
    }

    public static IReadOnlyList<string> GetAllocators()
    {
        return new List<string> { "System" };
    }
}
