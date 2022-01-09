using System.Windows.Controls;
using System.Windows.Input;

namespace BSU.GUI.Components
{
    public partial class Repository : UserControl
    {
        public Repository()
        {
            InitializeComponent();
        }

        private void Preset_Click(object sender, MouseButtonEventArgs e)
        {
            ((Core.ViewModel.Repository)DataContext).Details.Execute(null);
        }
    }
}
