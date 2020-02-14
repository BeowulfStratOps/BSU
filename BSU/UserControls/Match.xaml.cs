using System.Windows;
using System.Windows.Controls;

namespace BSU.GUI.UserControls
{
    public partial class Match : UserControl
    {
        public Match()
        {
            InitializeComponent();
        }

        private void MenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            var match = DataContext as Core.View.Match;
            match.DoUpdate();
        }
    }
}