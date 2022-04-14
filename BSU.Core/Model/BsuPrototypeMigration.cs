using System;
using System.IO;
using Newtonsoft.Json;

namespace BSU.Core.Model;

internal static class BsuPrototypeMigration
{
    public static DirectoryInfo? TryGetDownloadLocation()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var settingsPath = Path.Combine(localAppData, "BeowulfSync", "data.json");
        if (!File.Exists(settingsPath))
            return null;
        var json = File.ReadAllText(settingsPath);
        var settings = JsonConvert.DeserializeObject<PrototypePersistentSettings>(json);
        var modPath = new DirectoryInfo(settings.ModPath);
        return modPath.Exists ? modPath : null;
    }

    private class PrototypePersistentSettings
    {
        public string ModPath { get; set; } = "";
    }
}
