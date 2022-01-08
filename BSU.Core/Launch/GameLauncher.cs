using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using BSU.Core.Concurrency;
using BSU.Core.Model;
using NLog;

namespace BSU.Core.Launch;

internal static class GameLauncher
{
    private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();

    public static GameLaunchHandle Launch(IModelRepository preset, IEventBus eventBus)
    {
        var modPaths = CollectModPaths(preset, out var missingDlcs);

        if (missingDlcs.Any())
            return new GameLaunchHandle("Missing CDLCs: " + string.Join(", ", missingDlcs));

        var settings = preset.Settings;

        var parameters = BuildParameters(settings) + " \"-mod=" + string.Join(";", modPaths) + " \"";

        var exe = settings.X64 ? "arma3_x64.exe" : "arma3.exe";

        if (settings.BattlEye)
        {
            parameters = $"2 1 0 -exe {exe} {parameters}";
            exe = "arma3battleye.exe";
        }

        if (settings.ArmaPath == null)
            return new GameLaunchHandle("Failed to find Arma install path.");

        var absExePath = Path.Combine(settings.ArmaPath, exe);

        if (!File.Exists(absExePath))
            return new GameLaunchHandle("Failed to find Arma executable.");

        Logger.Info($"Launching with exe {absExePath} and parameters '{parameters}'");

        var procInfo = new ProcessStartInfo(absExePath, parameters)
        {
            WorkingDirectory = settings.ArmaPath
        };

        Process process;
        try
        {
            process = Process.Start(procInfo) ?? throw new NullReferenceException("Process is null");
        }
        catch (Exception e)
        {
            Logger.Error(e);
            return new GameLaunchHandle("Failed to start Arma.");
        }

        return new GameLaunchHandle(process, eventBus);
    }

    private static List<string> CollectModPaths(IModelRepository modelRepository, out List<string> missingDlcs)
    {
        var result = new List<string>();
        missingDlcs = new List<string>();

        // need mods and cdlcs
        foreach (var cdlc in modelRepository.GetServerInfo().CDLCs)
        {
            if (ArmaData.IsCDlcInstalled(cdlc))
                result.Add(ArmaData.CDlcMap[cdlc]);
            else
                missingDlcs.Add(ArmaData.CDlcMap[cdlc]);
        }

        foreach (var mod in modelRepository.GetMods())
        {
            if (mod.GetCurrentSelection() is ModSelectionStorageMod storageMod)
                result.Add(storageMod.StorageMod.GetAbsolutePath());
        }

        return result;
    }

    private static string BuildParameters(PresetSettings settings)
    {
        var result = new List<string> { "-noSplash" };

        if (settings.HugePages)
            result.Add("-hugepages");
        if (settings.WorldEmpty)
            result.Add("-world=empty");
        if (settings.ShowScriptErrors)
            result.Add("-showScriptErrors");
        result.Add("-name=" + settings.Profile);

        switch (settings.Allocator)
        {
            case "System":
                break;
            default:
                throw new ArgumentException();
        }

        // TODO: connect, port, password

        return string.Join(" ", result);
    }
}

public class GameLaunchHandle
{
    private readonly Process? _process;
    public readonly string? FailedReason;

    internal GameLaunchHandle(Process process, IEventBus eventBus)
{
        _process = process;
        process.EnableRaisingEvents = true;
        process.Exited += (_, _) => eventBus.ExecuteSynchronized(() => { Exited?.Invoke(); });
    }

    internal GameLaunchHandle(string failedReason)
    {
        FailedReason = failedReason;
    }

    public bool Succeeded => _process != null;

    internal event Action? Exited;

    internal static void Stop()
    {
        var armaProcesses = Process.GetProcesses().Where(p => p.ProcessName.ToLowerInvariant().Contains("arma")).ToList();

        foreach (var process in armaProcesses)
        {
            if (process == null) throw new InvalidOperationException();
            if (!process.CloseMainWindow())
                process.Kill(true);
        }
    }
}
