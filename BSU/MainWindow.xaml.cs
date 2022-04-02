using System.Windows.Input;
using BSU.Core.ViewModel;
using BSU.GUI.Components;

namespace BSU.GUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        public MainWindow()
        {
            TitleBarContent = new Menu(); // no idea how to this from xaml
            InitializeComponent();
        }

        private void OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            switch (e.ChangedButton)
            {
                case MouseButton.XButton1:
                    ((ViewModel)DataContext).Navigator.Back();
                    break;
                case MouseButton.XButton2:
                    ((ViewModel)DataContext).Navigator.Forward();
                    break;
            }
        }
    }
}
