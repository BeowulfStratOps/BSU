using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using BSU.Core.ViewModel;
using BSU.GUI.Actions;
using BSU.GUI.Dialogs;
using NLog;
using NLog.Config;
using NLog.Targets;
using Squirrel;

namespace BSU.GUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly Core.Core _core;
        private readonly ILogger _logger = LogManager.GetCurrentClassLogger();

        public MainWindow()
        {
            Thread.CurrentThread.Name = "main";
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            try
            {
                var assemblyLocation = Assembly.GetExecutingAssembly().Location;
                var settingsLocation = Path.Combine(Directory.GetParent(assemblyLocation).Parent.FullName, "settings.json");
                var interactionService = new InteractionService(this);
                _core = new Core.Core(new FileInfo(settingsLocation), interactionService);
                DataContext = _core.GetViewModel();
            }
            catch (Exception e)
            {
                _logger.Error(e);
                throw;
            }
            InitializeComponent();
        }

        private void MainWindow_OnClosing(object sender, CancelEventArgs e)
        {
            _core.Dispose();
        }

        private void ShowLogs_Click(object sender, RoutedEventArgs e)
        {
            var logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
            Process.Start("explorer.exe", logPath);
        }

        private void About_Click(object sender, RoutedEventArgs e)
        {
            new AboutDialog().ShowDialog();
        }

        private void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
        {

        }

        private async Task Update()
        {
#if DEBUG
            return;
#endif

            try
            {
                using var mgr = new UpdateManager("https://beowulfstratops.github.io/BSU-releases/files/");
                await mgr.UpdateApp();
            }
            catch (Exception e)
            {
                _logger.Error(e);
            }
        }

        private void OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            switch (e.ChangedButton)
            {
                case MouseButton.XButton1:
                    ((ViewModel)DataContext).Navigator.Back();
                    break;
                case MouseButton.XButton2:
                    ((ViewModel)DataContext).Navigator.Forward();
                    break;
            }
        }
    }
}
