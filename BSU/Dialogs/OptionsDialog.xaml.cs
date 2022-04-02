using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using BSU.Core.ViewModel;
using BSU.Core.ViewModel.Util;

namespace BSU.GUI.Dialogs;

public partial class OptionsDialog
{
    public OptionsDialog(string message, string title, Dictionary<object, string> options, MessageImageEnum image)
    {
        DataContext = this;

        Title = title;
        Message = message;
        Image = image;

        Options = new List<OptionsDialogOption>();

        foreach (var (option, optionText) in options)
        {
            Options.Add(new OptionsDialogOption(optionText, new DelegateCommand(() =>
            {
                Result = option;
                DialogResult = true;
            })));
        }

        InitializeComponent();
        Owner = Application.Current.MainWindow;
    }

    public object? Result { get; private set; }

    public string Message { get; }
    public MessageImageEnum Image { get; }

    public List<OptionsDialogOption> Options { get; }
}

public record OptionsDialogOption(string Text, ICommand Click);
