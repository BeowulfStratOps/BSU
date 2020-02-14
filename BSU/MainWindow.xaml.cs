﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using BSU.Core.View;

namespace BSU.GUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly Core.Core _core;
        
        private readonly object _lock = new object();
        
        public MainWindow()
        {
            var settingsFile = new FileInfo(Path.Combine(Directory.GetCurrentDirectory(), "settings.json"));
            _core = new Core.Core(settingsFile, action =>
            {
                //Console.WriteLine("Enter lock");
                lock (_lock)
                {
                    if (Application.Current.Dispatcher.CheckAccess())
                        action();
                    else
                        Application.Current.Dispatcher.Invoke(action);   
                }
                //Console.WriteLine("Left lock");
            });
            DataContext = _core;
            new Thread(_core.Load).Start();
            InitializeComponent();
        }

        private void ButtonBase_OnClick(object e, RoutedEventArgs routedEventArgs)
        {
            
        }

        private void MainWindow_OnClosing(object sender, CancelEventArgs e)
        {
            _core.Dispose();
        }
    }
}
