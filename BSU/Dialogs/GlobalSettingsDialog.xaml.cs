﻿using System.Windows;
using BSU.Core.ViewModel;

namespace BSU.GUI.Dialogs;

public partial class GlobalSettingsDialog
{
    public GlobalSettingsDialog(GlobalSettings viewModel)
    {
        DataContext = viewModel;
        InitializeComponent();
        Owner = Application.Current.MainWindow;
    }

    private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
    }
}
