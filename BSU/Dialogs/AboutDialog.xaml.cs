using System.Diagnostics;
using System.Reflection;
using System.Windows.Navigation;

namespace BSU.GUI.Dialogs
{
    public partial class AboutDialog
    {
        public AboutDialog()
        {
            InitializeComponent();
            Version.Text = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "Unknown";
        }

        private void Hyperlink_OnRequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
            e.Handled = true;
        }
    }
}
