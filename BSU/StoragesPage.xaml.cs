using System.Windows.Input;
using BSU.Core.ViewModel;

namespace BSU.GUI
{
    public partial class StoragesPage
    {
        public StoragesPage()
        {
            InitializeComponent();
        }

        private void Back_Click(object sender, MouseButtonEventArgs e)
        {
            ((StoragePage)DataContext).Back.Execute(null);
        }
    }
}
