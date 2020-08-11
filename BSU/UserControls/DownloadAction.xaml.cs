using System.Windows;
using System.Windows.Controls;

namespace BSU.GUI.UserControls
{
    public partial class DownloadAction : UserControl
    {
        public DownloadAction()
        {
            InitializeComponent();
        }

        private void MenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            var match = DataContext as Core.ViewModel.DownloadAction;
            match.DoDownload();
        }
    }
}