using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BSU.BSO.FileStructures;
using BunnyCDN.Net.Storage;
using Newtonsoft.Json;

namespace BSU.Server;

public static class PresetUpdater
{
    private record ModUpdatePaths(string Name, ISourceMod Source, IDestinationMod Destination);

    public static void UpdatePreset(PresetConfig config, bool dryRun, ChangedFileTracker changedFiles)
    {
        var modUpdates = new List<ModUpdatePaths>();

        foreach (var modName in config.ModList)
        {
            var sourcePath = new DirectoryInfo(Path.Combine(config.SourcePath, modName));

            if (!sourcePath.Exists)
                throw new DirectoryNotFoundException($"Directory {sourcePath} does not exist");
            var sourceMod = new LocalSourceMod(sourcePath);

            var destinationMod = GetDestinationMod(config, modName, dryRun, changedFiles);

            modUpdates.Add(new ModUpdatePaths(sourcePath.Name, sourceMod, destinationMod));
        }

        var stats = new List<ModUpdateStats>();

        foreach (var (name, sourceMod, destinationMod) in modUpdates)
        {
            var modStats = ModUpdater.UpdateMod(name, sourceMod, destinationMod);
            stats.Add(modStats);
        }

        var serverFile = BuildServerFile(config);
        var serverFileJson = JsonConvert.SerializeObject(serverFile, Formatting.Indented);
        WriteServerFile(config, serverFileJson, dryRun, changedFiles);

        PrintStats(stats);
    }

    private static void PrintStats(List<ModUpdateStats> stats)
    {
        foreach (var entry in stats)
        {
            Console.WriteLine(entry.ModName);
            Console.WriteLine("  New:     " + entry.New);
            Console.WriteLine("  Updated: " + entry.Updated);
            Console.WriteLine("  Deleted: " + entry.Deleted);
        }
    }

    private static void WriteServerFile(PresetConfig config, string serverFileJson, bool dryRun,
        ChangedFileTracker changedFileTracker)
    {
        changedFileTracker.AddChangedFilePath("/" + config.ServerFileName);

        if (config.BunnyCdn != null)
        {
            var path = $"/{config.BunnyCdn.ZoneName}/{config.ServerFileName}";
            if (dryRun)
            {
                Console.WriteLine($"Would write {path}");
                return;
            }

            var storage = new BunnyCDNStorage(config.BunnyCdn.ZoneName, config.BunnyCdn.ApiKey, config.BunnyCdn.Region);

            storage.UploadAsync(Util.StringToStream(serverFileJson), path).GetAwaiter().GetResult();

            return;
        }

        var serverFilePath = Path.Combine(config.DestinationPath, config.ServerFileName);

        if (dryRun)
        {
            Console.WriteLine($"Would write {serverFilePath}");
            return;
        }

        File.WriteAllText(serverFilePath, serverFileJson);
    }

    private static IDestinationMod GetDestinationMod(PresetConfig config, string modName, bool dryRun,
        ChangedFileTracker changedFileTracker)
    {
        if (config.BunnyCdn != null)
            return new BunnyCdnDestination(config.BunnyCdn, modName, dryRun, changedFileTracker);

        var destinationPath = new DirectoryInfo(Path.Combine(config.DestinationPath, modName));

        if (!destinationPath.Exists)
        {
            if (dryRun)
                Console.WriteLine($"Would create folder {destinationPath}");
            else
                destinationPath.Create();
        }

        return new LocalDestinationMod(destinationPath, dryRun, changedFileTracker);
    }

    private static ServerFile BuildServerFile(PresetConfig config)
    {
        return new ServerFile
        {
            Dlcs = config.DlcIds,
            ModFolders = config.ModList.Select(name => new ModFolder(name)).ToList(),
            Password = config.ServerPassword,
            ServerAddress = config.ServerAddress,
            ServerName = config.PresetName,
            ServerPort = config.ServerPort,
            SyncUris = config.SyncUris,
            LastUpdateDate = DateTime.UtcNow
        };
    }
}
