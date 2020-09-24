using System;
using System.Windows;
using BSU.Core.ViewModel;
using Microsoft.Win32;

namespace BSU.GUI.Dialogs
{
    public partial class AddStorageDialog : Window
    {
        public AddStorageDialog(AddStorage viewModel)
        {
            DataContext = viewModel;
            InitializeComponent();
            Owner = Application.Current.MainWindow;
        }

        private void Ok_OnClick(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        private void Path_OnClick(object sender, RoutedEventArgs e)
        {
            throw new NotImplementedException();
        }
    }
}