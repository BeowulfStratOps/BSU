using System.Windows.Input;
using BSU.Core.ViewModel;

namespace BSU.GUI
{
    public partial class RepositoryPage
    {
        public RepositoryPage()
        {
            InitializeComponent();
        }

        private void Back_Click(object sender, MouseButtonEventArgs e)
        {
            ((Repository)DataContext).Back.Execute(null);
        }

        private void Storages_Click(object sender, MouseButtonEventArgs e)
        {
            ((Repository)DataContext).ShowStorage.Execute(null);
        }
    }
}
