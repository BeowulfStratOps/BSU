using System.Windows;
using BSU.Core.ViewModel;

namespace BSU.GUI.Dialogs;

public partial class MessageDialog
{
    public MessageDialog(string message, string title, MessageImageEnum image)
    {
        DataContext = this;

        Title = title;
        Message = message;
        Image = image;

        InitializeComponent();
        Owner = Application.Current.MainWindow;
    }

    public string Message { get; }
    public MessageImageEnum Image { get; }

    private void Ok_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
    }
}
