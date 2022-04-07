using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using BSU.Core.Events;
using BSU.GUI.Actions;
using NLog;

namespace BSU.GUI
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App
    {
        private readonly ILogger _logger = LogManager.GetCurrentClassLogger();

        protected override void OnStartup(StartupEventArgs startupEventArgs)
        {
            var assemblyLocation = Assembly.GetExecutingAssembly().Location;
            var parentPath = Directory.GetParent(assemblyLocation)!.Parent!.FullName;

            var branchFilePath = Path.Combine(parentPath, "branch");
            var squirrel = new SquirrelHelper(branchFilePath);

            squirrel.HandleEvents();
            base.OnStartup(startupEventArgs);
            var mainWindow = new MainWindow();
            Thread.CurrentThread.Name = "main";
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

            var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "Unknown";
            _logger.Info($"BSU Version {version}");

            try
            {
                var settingsLocation = Path.Combine(parentPath, "settings.json");
                var interactionService = new InteractionService(mainWindow);

                var dispatcher = new SimpleDispatcher(Dispatcher.CurrentDispatcher);
                var themeService = new ThemeService(Resources, Path.Combine(parentPath, "themes"));
                var core = new Core.Core(new FileInfo(settingsLocation), interactionService, dispatcher, themeService);
                var vm = core.GetViewModel();

                void ShowNotification(string message)
                {
                    dispatcher.ExecuteSynchronized(() =>
                    {
                        core.EventManager.Publish(new NotificationEvent(message));
                    });
                }
                void ShowError(string message)
                {
                    dispatcher.ExecuteSynchronized(() =>
                    {
                        core.EventManager.Publish(new ErrorEvent(message));
                    });
                }
                squirrel.Update(ShowNotification, ShowError);

                mainWindow.DataContext = vm;
                mainWindow.Show();
                core.Dispose();
            }
            catch (Exception e)
            {
                _logger.Error(e);
                throw;
            }
        }
    }
}
