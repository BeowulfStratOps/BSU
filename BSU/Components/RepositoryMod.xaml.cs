using System;
using System.Windows.Controls;
using System.Windows.Input;

namespace BSU.GUI.Components
{
    public partial class RepositoryMod : UserControl
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
            if (_registeredMouseDown)
                repoMod.Actions.Open.Execute(null);
            _registeredMouseDown = false;
        }
    }
}
