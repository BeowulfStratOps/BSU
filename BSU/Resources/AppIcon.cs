using System;
using System.Drawing;
using System.Reflection;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Markup;
using System.Windows.Media.Imaging;

namespace BSU.GUI.Resources;

public class AppIcon : MarkupExtension
{
    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        using var ico = Icon.ExtractAssociatedIcon(Assembly.GetExecutingAssembly().Location);
        return Imaging.CreateBitmapSourceFromHIcon(ico.Handle, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
    }
}
