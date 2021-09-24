using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Shapes;

namespace BSU.GUI.UserControls
{
    public partial class SvgButton : UserControl
    {
        public static readonly DependencyProperty CommandProperty = DependencyProperty.Register("Command", typeof(ICommand), typeof(SvgButton), new PropertyMetadata(default(ICommand)));
        public static readonly DependencyProperty SvgProperty = DependencyProperty.Register("Svg", typeof(Path), typeof(SvgButton), new PropertyMetadata(default(Path)));

        public SvgButton()
        {
            InitializeComponent();
        }

        public ICommand Command
        {
            get => (ICommand)GetValue(CommandProperty);
            set => SetValue(CommandProperty, value);
        }

        public Path Svg
        {
            get => (Path)GetValue(SvgProperty);
            set => SetValue(SvgProperty, value);
        }
    }
}
