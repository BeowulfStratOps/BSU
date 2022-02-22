using System.Windows.Input;

namespace BSU.GUI.Components
{
    public partial class RepositoryMod
    {
        public RepositoryMod()
        {
            InitializeComponent();
        }

        private bool _registeredMouseDown;

        private void SelectedMouseDown(object sender, MouseButtonEventArgs e)
        {
            _registeredMouseDown = true;
        }

        private void SelectedMouseUp(object sender, MouseButtonEventArgs e)
        {
            var repoMod = (Core.ViewModel.RepositoryMod)DataContext;
            _registeredMouseDown = false;
            if (!_registeredMouseDown) return;
            if (!repoMod.Actions.Open.CanExecute(null)) return;
            repoMod.Actions.Open.Execute(null);
        }
    }
}
