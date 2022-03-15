using System;
using System.Windows;
using System.Windows.Media;

namespace BSU.GUI;

public class Theme
{
    public static Brush GetBrush(string key)
    {
        var app = Application.Current;
        return (Brush)(app.FindResource(key) ?? throw new NullReferenceException($"Brush for {key} was null"));
    }
}
