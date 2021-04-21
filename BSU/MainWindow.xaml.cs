﻿using System;
using System.ComponentModel;
using System.IO;
using System.Threading;
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
            Thread.CurrentThread.Name = "main";
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
