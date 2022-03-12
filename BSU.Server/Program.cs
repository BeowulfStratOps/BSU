﻿using System;
using System.IO;
using Newtonsoft.Json;

namespace BSU.Server;

public class Program
{
    private static void PrintUsage()
    {
        Console.WriteLine(@"Usage:
Update a preset: ./BSU.Server <path to config file>
Dry run update: ./BSU.Server dryrun <path to config file>
Print empty config:   ./BSU.Server template");
    }

    private static void PrintEmptyConfig()
    {
        var config = new PresetConfig();
        var json = JsonConvert.SerializeObject(config, Formatting.Indented);
        Console.WriteLine(json);
    }

    public static int Main(string[] args)
    {
        var dryRun = false;
        string configPath;

        switch (args.Length)
        {
            case 1 when args[0].ToLowerInvariant() == "template":
                PrintEmptyConfig();
                return 0;
            case 1:
                configPath = args[0];
                break;
            case 2 when args[0].ToLowerInvariant() == "dryrun":
                configPath = args[1];
                dryRun = true;
                break;
            default:
                PrintUsage();
                return 1;
        }

        PresetConfig config;
        try
        {
            var configJson = File.ReadAllText(configPath);
            config = JsonConvert.DeserializeObject<PresetConfig>(configJson);
        }
        catch (FileNotFoundException)
        {
            Console.WriteLine($"Couldn't find file {args[0]}");
            return 2;
        }

        try
        {
            PresetUpdater.UpdatePreset(config, dryRun);
            return 0;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return 3;
        }
    }
}
