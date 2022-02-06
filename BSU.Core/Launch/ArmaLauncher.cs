using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;
using BSU.Core.Launch.BiFileTypes;
using Newtonsoft.Json;
using NLog;

namespace BSU.Core.Launch;

public static class ArmaLauncher
{
    private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();

    private static string LauncherDirectory =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ArmA 3 Launcher");

    private static string PresetDirectory => Path.Combine(LauncherDirectory, "Presets");

    private static readonly SemaphoreSlim FileSystemLock = new(1);

    public static async Task<bool> UpdatePreset(string presetName, List<string> modFolders, List<string> dlcIds)
    {
        await FileSystemLock.WaitAsync();

        try
        {
            var local = await ReadLocal();
            var preset = await ReadPreset(presetName);

            if (CheckLocalIsUpToData(local, modFolders) && preset != null && CheckPresetIsUpToDate(preset, modFolders, dlcIds))
                return false;

            UpdateLocal(modFolders, local);
            await WriteLocal(local);
            await WritePreset(presetName, modFolders, dlcIds);

            return true;
        }
        finally
        {
            FileSystemLock.Release();
        }
    }

    private static bool CheckPresetIsUpToDate(Preset2 preset, List<string> modFolders, List<string> dlcIds)
    {
        return dlcIds.ToHashSet().SetEquals(preset.DlcIds) &&
               modFolders.Select(ModFolderToPublishedId).ToHashSet().SetEquals(preset.PublishedId);
    }

    private static bool CheckLocalIsUpToData(Local local, List<string> modFolders)
    {
        return !modFolders.Except(local.KnownLocalMods, StringComparer.InvariantCultureIgnoreCase).Any() &&
               !modFolders.Except(local.UserDirectories, StringComparer.InvariantCultureIgnoreCase).Any();
    }

    private static async Task<Preset2?> ReadPreset(string presetName)
    {
        var presetPath = Path.Combine(PresetDirectory, $"{presetName}.preset2");

        if (!File.Exists(presetPath))
            return null;

        var xmlSerializer = new XmlSerializer(typeof(Preset2));
        var serialized = await File.ReadAllBytesAsync(presetPath);
        await using var memStream = new MemoryStream(serialized);
        using var reader = new StreamReader(memStream, Encoding.UTF8);
        return (Preset2)(xmlSerializer.Deserialize(memStream) ?? throw new InvalidDataException());
    }

    private static string ModFolderToPublishedId(string modFolder) => $"local:{modFolder.ToUpperInvariant()}";

    private static async Task WritePreset(string presetName, List<string> modFolders, List<string> dlcIds)
    {
        var preset = new Preset2
        {
            LastUpdated = DateTime.UtcNow,
            PublishedId = new List<string>(),
            DlcIds = dlcIds
        };

        foreach (var modFolder in modFolders)
        {
            preset.PublishedId.Add(ModFolderToPublishedId(modFolder));
        }

        var xmlSerializer = new XmlSerializer(typeof(Preset2));

        var presetPath = Path.Combine(PresetDirectory, $"{presetName}.preset2");

        await using var memoryStream = new MemoryStream();
        await using var writer = new StreamWriter(memoryStream, Encoding.UTF8);

        xmlSerializer.Serialize(writer, preset);

        await using var presetStream = new FileStream(presetPath, FileMode.Create);
        memoryStream.Position = 0;
        await memoryStream.CopyToAsync(presetStream);

        Logger.Info($"Wrote launcher preset to {presetPath}");
    }

    private static async Task<Local> ReadLocal()
    {
        var localPath = Path.Combine(LauncherDirectory, "Local.json");
        Logger.Info($"Reading local.json from {localPath}");

        if (!File.Exists(localPath))
        {
            Logger.Warn("Local.json does not exist");
            return new Local
            {
                DateCreated = DateTime.Now,
                AutodetectionDirectories = new List<string>(),
                KnownLocalMods = new List<string>(),
                UserDirectories = new List<string>()
            };
        }

        var json = await File.ReadAllTextAsync(localPath);

        return JsonConvert.DeserializeObject<Local>(json);
    }

    private static void UpdateLocal(List<string> modFolders, Local local)
    {
        foreach (var modFolder in modFolders)
        {
            if (!local.KnownLocalMods.Exists(x => x.ToLower() == modFolder))
            {
                local.KnownLocalMods.Add(modFolder);
            }

            if (!local.UserDirectories.Exists(x => x.ToLower() == modFolder))
            {
                local.UserDirectories.Add(modFolder);
            }
        }
    }

    private static async Task WriteLocal(Local local)
    {
        var settings = new JsonSerializerSettings()
        {
            DateFormatHandling = DateFormatHandling.MicrosoftDateFormat
        };

        var localPath = Path.Combine(LauncherDirectory, "Local.json");
        var json = JsonConvert.SerializeObject(local, Formatting.None, settings);
        await File.WriteAllTextAsync(localPath, json);
        Logger.Info($"Wrote local.json to {localPath}");
    }
}
