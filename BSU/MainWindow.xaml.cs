using System;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using NLog;
using NLog.Config;
using NLog.Targets;

namespace BSU.GUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly Core.Core _core;
        
        public MainWindow()
        {
            var config = new LoggingConfiguration();
            var logfile = new FileTarget("logfile") // TODO: use nlog.config
            {
                FileName = "C:\\arma3\\BSU\\log.log",
                DeleteOldFileOnStartup = true
            };
            config.AddRule(LogLevel.Trace, LogLevel.Fatal, logfile);
            LogManager.Configuration = config;
            
            var settingsFile = new FileInfo(Path.Combine(Directory.GetCurrentDirectory(), "settings.json"));
            _core = new Core.Core(settingsFile, action => Dispatcher.BeginInvoke(DispatcherPriority.Background, action));
            DataContext = _core.ViewModel;
            InitializeComponent();
            Dispatcher.InvokeAsync(Run);
        }

        private async Task Run()
        {
            try
            {
                await _core.Start();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                Close();
            }
        }

        private void MainWindow_OnClosing(object sender, CancelEventArgs e)
        {
            _core.Dispose();
        }
    }
}
