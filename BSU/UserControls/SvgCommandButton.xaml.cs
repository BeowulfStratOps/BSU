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
    public partial class SvgCommandButton : UserControl, INotifyPropertyChanged
    {
        public SvgCommandButton()
        {
            InitializeComponent();
        }

        public IStateCommand Command
        {
            get => (IStateCommand)GetValue(CommandProperty);
            set => SetValue(CommandProperty, value);
        }

        private void UpdateState()
        {
            var state = Command?.State ?? CommandState.Loading;
            switch (state)
            {
                case CommandState.Loading:
                    ButtonEnabled = false;
                    Svg.Fill = new SolidColorBrush(Colors.Black);
                    ButtonVisible = Visibility.Hidden;
                    SpinnerVisible = Visibility.Visible;
                    break;
                case CommandState.Disabled:
                    ButtonEnabled = false;
                    Svg.Fill = new SolidColorBrush(Colors.Gray);
                    ButtonVisible = Visibility.Visible;
                    SpinnerVisible = Visibility.Hidden;
                    break;
                case CommandState.Warning:
                    ButtonEnabled = true;
                    Svg.Fill = new SolidColorBrush(Colors.Orange);
                    ButtonVisible = Visibility.Visible;
                    SpinnerVisible = Visibility.Hidden;
                    break;
                case CommandState.Enabled:
                    ButtonEnabled = true;
                    Svg.Fill = new SolidColorBrush(Colors.Black);
                    ButtonVisible = Visibility.Visible;
                    SpinnerVisible = Visibility.Hidden;
                    break;
                case CommandState.Primary:
                    ButtonEnabled = true;
                    Svg.Fill = new SolidColorBrush(Colors.Green);
                    ButtonVisible = Visibility.Visible;
                    SpinnerVisible = Visibility.Hidden;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public Path Svg
        {
            get => (Path)GetValue(SvgProperty);
            set => SetValue(SvgProperty, value);
        }

        private Visibility _buttonVisible;
        public Visibility ButtonVisible
        {
            get => _buttonVisible;
            private set
            {
                if (_buttonVisible == value) return;
                _buttonVisible = value;
                OnPropertyChanged();
            }
        }

        private Visibility _spinnerVisible;
        public Visibility SpinnerVisible
        {
            get => _spinnerVisible;
            private set
            {
                if (_spinnerVisible == value) return;
                _spinnerVisible = value;
                OnPropertyChanged();
            }
        }

        private bool _buttonEnabled;
        public bool ButtonEnabled
        {
            get => _buttonEnabled;
            private set
            {
                if (_buttonEnabled == value) return;
                _buttonEnabled = value;
                OnPropertyChanged();
            }
        }

        private ICommand _click;
        public ICommand Click
        {
            get => _click;
            private set
            {
                if (_click == value) return;
                _click = value;
                OnPropertyChanged();
            }
        }

        public static readonly DependencyProperty CommandProperty = DependencyProperty.Register("Command", typeof(IStateCommand), typeof(SvgCommandButton), new PropertyMetadata(default(IStateCommand), OnCommandChanged));
        public static readonly DependencyProperty SvgProperty = DependencyProperty.Register("Svg", typeof(Path), typeof(SvgCommandButton), new PropertyMetadata(default(Path)));

        public event PropertyChangedEventHandler PropertyChanged;

        private static void OnCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var oldValue = (IStateCommand)e.OldValue;
            var newValue = (IStateCommand)e.NewValue;
            var commandButton = (SvgCommandButton)d;
            commandButton.HandleCommandChange(oldValue, newValue);
        }

        private void HandleCommandChange(IStateCommand oldValue, IStateCommand newValue)
        {
            if (oldValue != null) oldValue.StateChanged -= UpdateState;
            if (newValue != null)
            {
                newValue.StateChanged += UpdateState;
                Click = new DelegateCommand(newValue.Execute);
            }
            else
            {
                Click = null;
            }

            UpdateState();
        }


        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
