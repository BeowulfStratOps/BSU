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
            base.OnStartup(startupEventArgs);
            var mainWindow = new MainWindow();
            Thread.CurrentThread.Name = "main";
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            try
            {
                var assemblyLocation = Assembly.GetExecutingAssembly().Location;
                var settingsLocation = Path.Combine(Directory.GetParent(assemblyLocation)!.Parent!.FullName, "settings.json");
                var interactionService = new InteractionService(mainWindow);

                var dispatcher = new SimpleDispatcher(Dispatcher.CurrentDispatcher);
                var core = new Core.Core(new FileInfo(settingsLocation), interactionService, dispatcher);
                var vm = core.GetViewModel();
                void ShowUpdateNotification(string message)
                {
                    dispatcher.ExecuteSynchronized(() =>
                    {
                        vm.AddNotification(new NotificationEvent(message));
                    });
                }
                UpdateHelper.Update(ShowUpdateNotification);
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
