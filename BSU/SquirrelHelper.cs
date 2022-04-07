using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NLog;
using Squirrel;

namespace BSU.GUI;

internal class SquirrelHelper
{
    private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();

    public SquirrelHelper(string branchFilePath)
    {
        var branch = ReadOrCreateBranchFile(branchFilePath);

        Logger.Info($"Using distribution branch '{branch}'");

        _updateUrl = $"https://bsu-distribution.bso.ovh/{branch}/";
    }

    private static string ReadOrCreateBranchFile(string branchFilePath)
    {
        const string defaultBranch = "stable";

        if (File.Exists(branchFilePath)) return File.ReadAllText(branchFilePath).Trim().ToLowerInvariant();

        File.WriteAllText(branchFilePath, defaultBranch);
        return defaultBranch;

    }

    private static bool ShouldUpdate()
    {
        if (Environment.GetCommandLineArgs().Any(a => a.ToLowerInvariant() == "noupdate"))
            return false;
        if (System.Diagnostics.Debugger.IsAttached)
            return false;
#if DEBUG
        return false;
#else
        return true;
#endif
    }

    private readonly string _updateUrl;

    public void HandleEvents()
    {
        using var mgr = new UpdateManager(_updateUrl);
        SquirrelAwareApp.HandleEvents(
            onInitialInstall: _ =>  mgr.CreateShortcutForThisExe(),
            onAppUpdate: _ => mgr.CreateShortcutForThisExe(),
            onAppUninstall: _ => mgr.RemoveShortcutForThisExe());
    }

    private async Task DoUpdate(Action<string> showUpdateNotification)
    {
        using var mgr = new UpdateManager(_updateUrl);

        var updates = await mgr.CheckForUpdate();
        if (!updates.ReleasesToApply.Any())
            return;
        showUpdateNotification("Downloading and installing a new BSU version...");
        await mgr.UpdateApp();
        showUpdateNotification("Update complete - Please restart BSU.");
    }

    public void Update(Action<string> showUpdateNotification, Action<string> showError)
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
                showError("BSU Update failed.");
            }
        });
    }
}
