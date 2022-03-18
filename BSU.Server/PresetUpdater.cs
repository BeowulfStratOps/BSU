using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BSU.BSO.FileStructures;
using Newtonsoft.Json;

namespace BSU.Server;

public static class PresetUpdater
{
    private record ModUpdatePaths(string Name, ISourceMod Source, IDestinationMod Destination);

    public static void UpdatePreset(PresetConfig config, bool dryRun)
    {
        var modUpdates = new List<ModUpdatePaths>();

        foreach (var modName in config.ModList)
        {
            var sourcePath = new DirectoryInfo(Path.Combine(config.SourcePath, modName));

            if (!sourcePath.Exists)
                throw new DirectoryNotFoundException($"Directory {sourcePath} does not exist");
            var sourceMod = new LocalSourceMod(sourcePath);

            var destinationMod = GetDestinationMod(config, modName, dryRun);

            modUpdates.Add(new ModUpdatePaths(sourcePath.Name, sourceMod, destinationMod));
        }

        foreach (var (name, sourceMod, destinationMod) in modUpdates)
        {
            ModUpdater.UpdateMod(name, sourceMod, destinationMod);
        }

        var serverFile = BuildServerFile(config);
        var serverFileJson = JsonConvert.SerializeObject(serverFile, Formatting.Indented);
        var serverFilePath = Path.Combine(config.DestinationPath, config.ServerFileName);
        if (dryRun)
            Console.WriteLine($"Would write {serverFilePath}");
        else
            File.WriteAllText(serverFilePath, serverFileJson);
    }

    private static IDestinationMod GetDestinationMod(PresetConfig config, string modName, bool dryRun)
    {
        if (config.BunnyCdn != null)
            return new BunnyCdnDestination(config.BunnyCdn, modName, dryRun);

        var destinationPath = new DirectoryInfo(Path.Combine(config.DestinationPath, modName));

        if (!destinationPath.Exists)
        {
            if (dryRun)
                Console.WriteLine($"Would create folder {destinationPath}");
            else
                destinationPath.Create();
        }

        return new LocalDestinationMod(destinationPath, dryRun);
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
            ServerPort = config.ServerPort
        };
    }
}
