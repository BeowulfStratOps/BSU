﻿using System;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using BSU.GUI.Actions;
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
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            var settingsFile = new FileInfo(Path.Combine(Directory.GetCurrentDirectory(), "settings.json"));
            _core = new Core.Core(settingsFile);
            var viewModel = _core.ViewModel;
            viewModel.InteractionService = new InteractionService(this);
            DataContext = viewModel;
            InitializeComponent();
            viewModel.Run();
        }

        private void MainWindow_OnClosing(object sender, CancelEventArgs e)
        {
            _core.Dispose();
        }
    }
}
