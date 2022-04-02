using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using BSU.Core.ViewModel;
using BSU.GUI.Dialogs;

namespace BSU.GUI.Components;

public partial class Menu
{
    public Menu()
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

    private void Settings_Click(object sender, RoutedEventArgs e)
    {
        var vm = (ViewModel)DataContext;
        vm.Settings.Execute(null);
    }
}
