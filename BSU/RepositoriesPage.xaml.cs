using System.Windows.Input;

namespace BSU.GUI
{
    public partial class RepositoriesPage
    {
        public RepositoriesPage()
        {
            InitializeComponent();
        }

        private void Storages_Click(object sender, MouseButtonEventArgs e)
        {
            ((Core.ViewModel.RepositoriesPage)DataContext).ShowStorage.Execute(null);
        }
    }
}
