using System.Windows.Input;

namespace BSU.GUI.Components
{
    public partial class Storage
    {
        public Storage()
        {
            InitializeComponent();
        }

        private void ToggleShowMods(object sender, MouseButtonEventArgs e)
        {
            ((Core.ViewModel.Storage)DataContext).ToggleShowMods.Execute(null);
        }
    }
}
