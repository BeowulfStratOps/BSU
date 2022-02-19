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
        private SelectableModAction _vm = null!;

        private void BorderOnLoaded(object sender, RoutedEventArgs e)
        {
            _vm = (SelectableModAction)Border.DataContext;
            _isSelected = _vm.IsSelected;
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
            Border.Background = _isHovered ? new SolidColorBrush(Colors.CornflowerBlue) : SystemColors.WindowBrush;
        }

        private void VmOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(SelectableModAction.IsSelected)) return;
            _isSelected = _vm.IsSelected;
            UpdateColour();
        }

        private void Border_OnMouseEnter(object sender, MouseEventArgs e)
        {
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
            ((SelectableModAction)Border.DataContext).Select();
        }
    }
}
