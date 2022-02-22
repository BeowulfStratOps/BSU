using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Input;
using BSU.Core.ViewModel;
using BSU.GUI.Dialogs;

namespace BSU.GUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        public MainWindow()
        {
            InitializeComponent();
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

        private void Settings_Click(object sender, RoutedEventArgs e)
        {
            var vm = (ViewModel)DataContext;
            vm.Settings.Execute(null);
        }
    }
}
