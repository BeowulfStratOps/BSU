using System.Windows.Input;

namespace BSU.GUI.Components
{
    public partial class Repository
    {
        public Repository()
        {
            InitializeComponent();
        }

        private void Preset_Click(object sender, MouseButtonEventArgs e)
        {
            var command = ((Core.ViewModel.Repository)DataContext).Details;
            if (command.CanExecute(null))
                command.Execute(null);
        }
    }
}
