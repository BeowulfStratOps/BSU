using System.Windows.Input;

namespace BSU.GUI.Components
{
    public partial class RepositoryMod
    {
        public RepositoryMod()
        {
            InitializeComponent();
        }

        private bool _selectRegisteredMouseDown;
        private bool _expandRegisteredMouseDown;

        private void SelectedMouseDown(object sender, MouseButtonEventArgs e)
        {
            _selectRegisteredMouseDown = true;
            e.Handled = true;
        }

        private void SelectedMouseUp(object sender, MouseButtonEventArgs e)
        {
            var repoMod = (Core.ViewModel.RepositoryMod)DataContext;
            if (!_selectRegisteredMouseDown) return;
            _selectRegisteredMouseDown = false;
            e.Handled = true;
            if (!repoMod.Actions.Open.CanExecute(null)) return;
            repoMod.Actions.Open.Execute(null);
        }

        private void ToggleExpandMouseDown(object sender, MouseButtonEventArgs e)
        {
            _expandRegisteredMouseDown = true;
        }

        private void ToggleExpandMouseUp(object sender, MouseButtonEventArgs e)
        {
            var repoMod = (Core.ViewModel.RepositoryMod)DataContext;
            if (!_expandRegisteredMouseDown) return;
            _expandRegisteredMouseDown = false;
            e.Handled = true;
            if (!repoMod.ToggleExpand.CanExecute(null)) return;
            repoMod.ToggleExpand.Execute(null);
        }
    }
}
