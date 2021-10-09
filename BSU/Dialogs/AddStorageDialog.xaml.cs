using System;
using System.Windows;
using System.Windows.Forms;
using BSU.Core.ViewModel;
using BSU.Core.ViewModel.Util;
using Application = System.Windows.Application;

namespace BSU.GUI.Dialogs
{
    public partial class AddStorageDialog : Window, ICloseable
    {
        public AddStorageDialog(AddStorage viewModel)
        {
            DataContext = viewModel;
            InitializeComponent();
            Owner = Application.Current.MainWindow;
        }

        private void Path_OnClick(object sender, RoutedEventArgs e)
        {
            var dialog = new FolderBrowserDialog();
            var result = dialog.ShowDialog();
            if (result != System.Windows.Forms.DialogResult.OK) return;
            Path.Text = dialog.SelectedPath;
        }

        public void Close(bool result)
        {
            DialogResult = result;
        }
    }
}
