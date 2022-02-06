using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BSU.Core.Concurrency;
using BSU.Core.Model;
using NLog;

namespace BSU.Core.Launch;

internal static class GameLauncher
{
    private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();

    public static GameLaunchResult Launch(IModelRepository preset, IDispatcher dispatcher)
    {
        if (!preset.Settings.UseBsuLauncher) throw new InvalidOperationException();

        var modPaths = CollectModPaths(preset, out var missingDlcs);

        if (missingDlcs.Any())
            return new GameLaunchResult("Missing CDLCs: " + string.Join(", ", missingDlcs));

        var settings = preset.Settings;

        var parameters = BuildParameters(settings) + " \"-mod=" + string.Join(";", modPaths) + " \"";

        var gameExe = settings.X64 ? "arma3_x64.exe" : "arma3.exe";
        var exe = gameExe;

        if (settings.BattlEye)
        {
            parameters = $"2 1 0 -exe {gameExe} {parameters}";
            exe = "arma3battleye.exe";
        }

        if (settings.ArmaPath == null)
            return new GameLaunchResult("Failed to find Arma install path.");

        var absExePath = Path.Combine(settings.ArmaPath, exe);

        if (!File.Exists(absExePath))
            return new GameLaunchResult("Failed to find Arma executable.");

        Logger.Info($"Launching with exe {absExePath} and parameters '{parameters}'");

        var procInfo = new ProcessStartInfo(absExePath, parameters)
        {
            WorkingDirectory = settings.ArmaPath
        };

        try
        {
            if (settings.BattlEye)
            {
                var gameProcessFinder = BuildProcessFinder(gameExe);
                var beProcess = Process.Start(procInfo);
                if (beProcess != null) return new GameLaunchResult(new BattlEyeGameLaunchHandle(gameProcessFinder, dispatcher));

            }
            else
            {
                var process = Process.Start(procInfo);
                if (process != null) return new GameLaunchResult(new DirectGameLaunchHandle(process, dispatcher));
            }

            Logger.Error("Failed to launch: process is null");
            return new GameLaunchResult("Failed to start Arma.");
        }
        catch (Exception e)
        {
            Logger.Error(e);
            return new GameLaunchResult("Failed to start Arma.");
        }
    }

    private static Func<Process?> BuildProcessFinder(string exe)
    {
        exe = Path.GetFileNameWithoutExtension(exe);
        var processesBefore = Process.GetProcessesByName(exe).Select(p => p.Id).ToHashSet();

        Process? Check()
        {
            var current = Process.GetProcessesByName(exe);
            return current.FirstOrDefault(process => !processesBefore.Contains(process.Id));
        }

        return Check;
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

public class GameLaunchResult
{
    private readonly string? _failReason;
    private readonly GameLaunchHandle? _handle;

    public bool Succeeded => _handle != null;

    public string GetFailedReason()
    {
        if (_failReason == null) throw new InvalidOperationException();
        return _failReason;
    }

    public GameLaunchHandle GetHandle()
    {
        if (_handle == null) throw new InvalidOperationException();
        return _handle;
    }

    internal GameLaunchResult(string failedReason)
    {
        _failReason = failedReason;
    }

    internal GameLaunchResult(GameLaunchHandle handle)
    {
        _handle = handle;
    }
}

public abstract class GameLaunchHandle
{
    private readonly IDispatcher _dispatcher;

    protected GameLaunchHandle(IDispatcher dispatcher)
    {
        _dispatcher = dispatcher;
    }

    internal event Action? Exited;

    protected void OnExited()
    {
        _dispatcher.ExecuteSynchronized(() => { Exited?.Invoke(); });
    }

    internal abstract Task Stop();
}

public class DirectGameLaunchHandle : GameLaunchHandle
{
    private readonly Process _process;

    public DirectGameLaunchHandle(Process process, IDispatcher dispatcher) : base(dispatcher)
    {
        _process = process;
        process.EnableRaisingEvents = true;
        process.Exited += (_, _) => OnExited();
    }

    internal override Task Stop()
    {
        if (!_process.CloseMainWindow())
            _process.Kill(true);
        return Task.CompletedTask;
    }
}


internal class BattlEyeGameLaunchHandle : GameLaunchHandle
{
    private readonly Task<Process?> _findTask;

    internal BattlEyeGameLaunchHandle(Func<Process?> processFinder, IDispatcher dispatcher) : base(dispatcher)
    {
        _findTask = Task.Run(async () =>
        {
            for (int i = 0; i < 100; i++)
            {
                var process = processFinder();
                if (process == null)
                {
                    await Task.Delay(100);
                    continue;
                }

                process.EnableRaisingEvents = true;
                process.Exited += (_, _) => OnExited();
                return process;
            }

            LogManager.GetCurrentClassLogger().Error("Failed to find process after 10seconds of trying");
            OnExited();
            return null;
        });
    }

    internal override async Task Stop()
    {
        if (_findTask.IsFaulted) return;
        var process = await _findTask;
        if (process == null) return;
        if (!process.CloseMainWindow())
            process.Kill();
    }
}
