using System.Windows;
using BSU.Core.ViewModel;
using BSU.Core.ViewModel.Util;

namespace BSU.GUI.Dialogs
{
    public partial class AddRepositoryDialog : ICloseable
    {
        public AddRepositoryDialog(AddRepository viewModel)
        {
            DataContext = viewModel;
            InitializeComponent();
            Owner = Application.Current.MainWindow;
        }

        public void Close(bool result)
        {
            DialogResult = result;
        }
    }
}
