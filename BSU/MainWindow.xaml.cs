using System;
using System.ComponentModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using BSU.Core.ViewModel;
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
        private readonly ViewModel _viewModel;

        public MainWindow()
        {
            Thread.CurrentThread.Name = "main";
            var settingsFile = new FileInfo(Path.Combine(Directory.GetCurrentDirectory(), "settings.json"));
            _core = new Core.Core(settingsFile);
            _viewModel = _core.ViewModel;
            DataContext = _viewModel;
            InitializeComponent();
            Run(); // TODO: this is terrible. it should be awaited _somewhere_
        }

        private async void Run()
        {
            try
            {
                await _viewModel.Load();
            }
            catch (Exception e)
            {
                throw;
            }
        }

        private void MainWindow_OnClosing(object sender, CancelEventArgs e)
        {
            _core.Dispose();
        }
    }
}
