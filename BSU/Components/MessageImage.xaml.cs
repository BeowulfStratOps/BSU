using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using BSU.Core.ViewModel;

namespace BSU.GUI.Components;

public partial class MessageImage
{
    public static readonly DependencyProperty ImageProperty = DependencyProperty.Register("Image", typeof(MessageImageEnum), typeof(MessageImage), new PropertyMetadata(default(MessageImageEnum)));

    public MessageImage()
    {
        InitializeComponent();
    }

    public MessageImageEnum Image
    {
        get => (MessageImageEnum)GetValue(ImageProperty);
        set => SetValue(ImageProperty, value);
    }
}
