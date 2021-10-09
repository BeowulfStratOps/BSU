using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using BSU.Core.Annotations;
using BSU.Core.ViewModel.Util;

namespace BSU.GUI.UserControls
{
    public partial class SvgButton : UserControl, INotifyPropertyChanged
    {
        public static readonly DependencyProperty CommandProperty = DependencyProperty.Register("Command", typeof(ICommand), typeof(SvgButton), new PropertyMetadata(default(ICommand), CommandChangedCallback));
        public static readonly DependencyProperty SvgBrushProperty = DependencyProperty.Register("SvgBrush", typeof(Brush), typeof(SvgButton), new PropertyMetadata(new SolidColorBrush(Colors.Black), ColorChangedCallback));

        private static void ColorChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((SvgButton)d).ColorChange();
        }

        private void ColorChange()
        {
            Update(null, null);
        }

        private static void CommandChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((SvgButton)d).CommandChange((ICommand)e.OldValue, (ICommand)e.NewValue);
        }

        private void CommandChange(ICommand oldValue, ICommand newValue)
        {
            if (oldValue != null) oldValue.CanExecuteChanged -= Update;
            if (newValue != null)
            {
                newValue.CanExecuteChanged += Update;
                Update(null, null);
            }
        }

        private void Update(object sender, EventArgs e)
        {
            var value = Command?.CanExecute(null) ?? false;
            if (value)
            {
                Brush = SvgBrush;
                IsEnabled = true;
                if (HideIfDisabled) Visibility = Visibility.Visible;
            }
            else
            {
                Brush = new SolidColorBrush(Colors.Gray);
                IsEnabled = false;
                if (HideIfDisabled) Visibility = Visibility.Collapsed;
            }
        }

        public SvgButton()
        {
            InitializeComponent();
        }

        public ICommand Command
        {
            get => (ICommand)GetValue(CommandProperty);
            set => SetValue(CommandProperty, value);
        }

        public Path Svg { get; set; }

        public bool HideIfDisabled { get; set; }

        private Brush _brush = Brushes.Black;
        public static readonly DependencyProperty CommandParameterProperty = DependencyProperty.Register("CommandParameter", typeof(object), typeof(SvgButton), new PropertyMetadata(default(object)));

        public Brush Brush
        {
            get => _brush;
            private set
            {
                if (_brush == value) return;
                _brush = value;
                OnPropertyChanged();
            }
        }

        public Brush SvgBrush
        {
            get => (Brush)GetValue(SvgBrushProperty);
            set => SetValue(SvgBrushProperty, value);
        }

        public object CommandParameter
        {
            get => GetValue(CommandParameterProperty);
            set => SetValue(CommandParameterProperty, value);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
