using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace BSU.GUI;

public class FontIcon : TextBlock
{
    public FontIcon()
    {
        FontFamily = new FontFamily("Segoe MDL2 Assets");
        VerticalAlignment = VerticalAlignment.Center;
    }

    public string Icon
    {
        set => Text = ((char)int.Parse(value, NumberStyles.HexNumber)).ToString();
    }
}
