using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using BSU.Core.ViewModel.Util;

namespace BSU.GUI.UserControls
{
    public partial class SelectableAction
    {
        public SelectableAction()
        {
            InitializeComponent();
            Border.Loaded += BorderOnLoaded;
        }

        private bool _isSelected;
        private bool _isHovered;
        private bool _isEnabled;
        private SelectableModAction _vm = null!;

        private void BorderOnLoaded(object sender, RoutedEventArgs e)
        {
            _vm = (SelectableModAction)Border.DataContext;
            _isSelected = _vm.IsSelected;
            _isEnabled = _vm.IsEnabled;
            _vm.PropertyChanged += VmOnPropertyChanged;
            UpdateColour();
        }

        private void UpdateColour()
        {
            if (_isSelected)
            {
                Border.Background = SystemColors.HighlightBrush;
                return;
            }

            if (_isHovered)
            {
                Border.Background = new SolidColorBrush(Colors.CornflowerBlue);
                return;
            }

            Border.Background = SystemColors.WindowBrush;
        }

        private void VmOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(SelectableModAction.IsSelected)) return;
            _isSelected = _vm.IsSelected;
            UpdateColour();
        }

        private void Border_OnMouseEnter(object sender, MouseEventArgs e)
        {
            if (!_isEnabled) return;
            _isHovered = true;
            UpdateColour();
        }

        private void Border_OnMouseLeave(object sender, MouseEventArgs e)
        {
            _isHovered = false;
            UpdateColour();
        }

        private void Border_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (!_isEnabled) return;
            ((SelectableModAction)Border.DataContext).Select();
        }
    }
}
