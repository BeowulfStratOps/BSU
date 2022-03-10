using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BSU.BSO.FileStructures;
using Newtonsoft.Json;

namespace BSU.Server;

public static class PresetUpdater
{
    private record ModUpdatePaths(DirectoryInfo SourcePath, DirectoryInfo DestinationSource);

    public static void UpdatePreset(PresetConfig config, bool dryRun)
    {
        if (!new DirectoryInfo(config.DestinationPath).Exists)
            throw new FileNotFoundException($"Folder {config.DestinationPath} doesn't exist");

        var modUpdates = new List<ModUpdatePaths>();

        foreach (var modName in config.ModList)
        {
            var sourcePath = new DirectoryInfo(Path.Combine(config.SourcePath, modName));
            var destinationPath = new DirectoryInfo(Path.Combine(config.DestinationPath, modName));

            if (!sourcePath.Exists)
                throw new DirectoryNotFoundException($"Directory {sourcePath} does not exist");

            if (!destinationPath.Exists)
            {
                if (dryRun)
                    Console.WriteLine($"Would create folder {destinationPath}");
                else
                    destinationPath.Create();
            }

            modUpdates.Add(new ModUpdatePaths(sourcePath, destinationPath));
        }

        foreach (var (sourcePath, destinationPath) in modUpdates)
        {
            ModUpdater.UpdateMod(sourcePath, destinationPath, dryRun, config.ZsyncThreads);
        }

        var serverFile = BuildServerFile(config);
        var serverFileJson = JsonConvert.SerializeObject(serverFile, Formatting.Indented);
        var serverFilePath = Path.Combine(config.DestinationPath, config.ServerFileName);
        if (dryRun)
            Console.WriteLine($"Would write {serverFilePath}");
        else
            File.WriteAllText(serverFilePath, serverFileJson);
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
