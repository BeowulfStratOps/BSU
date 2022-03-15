using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using BSU.Core.Annotations;

namespace BSU.GUI.UserControls;

public partial class CommandLink : INotifyPropertyChanged
{
    public static readonly DependencyProperty CommandProperty = DependencyProperty.Register("Command", typeof(ICommand), typeof(CommandLink), new PropertyMetadata(default(ICommand), CommandChanged));

    private static void CommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        ((CommandLink)d).UpdateCommand((ICommand)e.NewValue);
    }

    private void UpdateCommand(ICommand? newCommand)
    {
        if (newCommand == null) return;
        newCommand.CanExecuteChanged += (_, _) => Update();
        Update();
    }

    private void Update()
    {
        var canExecute = Command != null && Command.CanExecute(null);
        Foreground = canExecute ? new SolidColorBrush(Colors.Black) : new SolidColorBrush(Colors.DimGray);
        Cursor = canExecute ? Cursors.Hand : Cursors.Arrow;
        Visibility = !canExecute && CollapseIfDisabled ? Visibility.Collapsed : Visibility.Visible;
    }

    public static readonly DependencyProperty TextProperty = DependencyProperty.Register("Text", typeof(string), typeof(CommandLink), new PropertyMetadata(default(string)));

    public CommandLink()
    {
        InitializeComponent();
    }

    public bool CollapseIfDisabled { get; set; }

    public ICommand? Command
    {
        get => (ICommand?)GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    private void OnClick(object sender, MouseButtonEventArgs e)
    {
        if (Command != null && Command.CanExecute(null))
            Command.Execute(null);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    [NotifyPropertyChangedInvocator]
    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
