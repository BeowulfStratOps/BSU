using System.Windows;
using System.Windows.Input;
using BSU.Core.ViewModel;

namespace BSU.GUI.Components;

public partial class NavigatorBar
{
    public static readonly DependencyProperty TitleProperty = DependencyProperty.Register("Title", typeof(string), typeof(NavigatorBar), new PropertyMetadata(default(string)));
    public static readonly DependencyProperty NavigatorProperty = DependencyProperty.Register("Navigator", typeof(INavigator), typeof(NavigatorBar), new PropertyMetadata(default(INavigator)));
    public static readonly DependencyProperty CanGoBackProperty = DependencyProperty.Register("CanGoBack", typeof(bool), typeof(NavigatorBar), new PropertyMetadata(true));
    public static readonly DependencyProperty CanGoToStoragesProperty = DependencyProperty.Register("CanGoToStorages", typeof(bool), typeof(NavigatorBar), new PropertyMetadata(true));

    public NavigatorBar()
    {
        InitializeComponent();
    }

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public INavigator Navigator
    {
        get => (INavigator)GetValue(NavigatorProperty);
        set => SetValue(NavigatorProperty, value);
    }

    public bool CanGoBack
    {
        get => (bool)GetValue(CanGoBackProperty);
        set => SetValue(CanGoBackProperty, value);
    }

    public bool CanGoToStorages
    {
        get => (bool)GetValue(CanGoToStoragesProperty);
        set => SetValue(CanGoToStoragesProperty, value);
    }

    private void Storages_Click(object sender, MouseButtonEventArgs e)
    {
        Navigator.NavigateToStorages();
    }

    private void Back_Click(object sender, MouseButtonEventArgs e)
    {
        Navigator.NavigateBack();
    }
}
