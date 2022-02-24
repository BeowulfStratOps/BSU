using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NLog;
using Squirrel;

namespace BSU.GUI;

internal static class UpdateHelper
{
    private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();

    private static bool ShouldUpdate()
    {
        if (Environment.GetCommandLineArgs().Any(a => a.ToLowerInvariant() == "noupdate"))
            return false;
        if (System.Diagnostics.Debugger.IsAttached)
            return false;
#if DEBUG
        return false;
#endif
        return true;
    }

    private static async Task DoUpdate(Action<string> showUpdateNotification)
    {
        using var mgr = new UpdateManager("https://bsu-distribution.bso.ovh/stable/");
        SquirrelAwareApp.HandleEvents(
            onInitialInstall: _ => UpdateShortcuts(mgr),
            onAppUpdate: _ => UpdateShortcuts(mgr),
            onAppUninstall: _ => RemoveShortcuts(mgr));
        var updates = await mgr.CheckForUpdate();
        if (!updates.ReleasesToApply.Any())
            return;
        showUpdateNotification("Downloading and installing a new BSU version...");
        await mgr.UpdateApp();
        showUpdateNotification("Updated BSU. Please restart.");
    }

    private static void RemoveShortcuts(UpdateManager updateManager)
    {
        updateManager.RemoveShortcutsForExecutable(GetExePath(), ShortcutLocation.Desktop);
        updateManager.RemoveShortcutsForExecutable(GetExePath(), ShortcutLocation.StartMenu);
    }

    private static void UpdateShortcuts(UpdateManager updateManager)
    {
        updateManager.CreateShortcutsForExecutable(GetExePath(), ShortcutLocation.Desktop, false);
        updateManager.CreateShortcutsForExecutable(GetExePath(), ShortcutLocation.StartMenu, false);
    }

    private static string GetExePath()
    {
        var entryAssembly = System.Reflection.Assembly.GetEntryAssembly()!;
        if (!entryAssembly.FullName!.ToLowerInvariant().Contains("BSU"))
        {
            Logger.Error($"Tried to create a shortcut for assembly {entryAssembly.FullName}. Aborting");
            throw new InvalidOperationException();
        }
        return Path.GetFileName(entryAssembly.Location);
    }

    public static void Update(Action<string> showUpdateNotification)
    {
        if (!ShouldUpdate())
            return;
        Task.Run(async () =>
        {
            try
            {
                await DoUpdate(showUpdateNotification);
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
        });
    }
}
