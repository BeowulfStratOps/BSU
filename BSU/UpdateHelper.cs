using System;
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
        var updates = await mgr.CheckForUpdate();
        if (!updates.ReleasesToApply.Any())
            return;
        showUpdateNotification("Downloading and installing new BSU version...");
        await mgr.UpdateApp();
        showUpdateNotification("Updated BSU. Please restart.");
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
