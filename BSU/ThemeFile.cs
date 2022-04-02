using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using NLog;
using Brush = System.Windows.Media.Brush;
using Color = System.Windows.Media.Color;
using FontFamily = System.Windows.Media.FontFamily;

namespace BSU.GUI;

internal static class ThemeFile
{
    private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();

    public static void CreateDefaultThemeFile(string themePath)
    {
        Logger.Info($"Writing default theme to {themePath}");

        var themeResourceDictionary = new ResourceDictionary
        {
            Source = new Uri(";component/Resources/Skin.xaml", UriKind.RelativeOrAbsolute)
        };

        var entries = new List<(string key, string value)>();

        // TODO: sort keys
        foreach (DictionaryEntry entry in themeResourceDictionary)
        {
            var (objKey, value) = entry;
            if (objKey is not string key) throw new InvalidDataException();

            switch (value)
            {
                case FontFamily font:
                    entries.Add((key, font.Source));
                    break;
                case SolidColorBrush brush:
                    entries.Add((key, ColorToHtml(brush.Color)));
                    break;
            }
        }

        using var writer = new StreamWriter(themePath);
        foreach (var (key, value) in entries.OrderBy(e => e.key))
        {
            writer.WriteLine($"{key}={value}");
        }
    }

    public static void LoadTheme(string themePath, ResourceDictionary target)
    {
        Logger.Info($"Loading theme from {themePath}");

        var themeText = File.ReadAllText(themePath);

        foreach (var line in themeText.Split("\n", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
        {
            if (line.StartsWith("#")) continue;
            var split = line.Split("=", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            if (split.Length != 2)
                throw new InvalidDataException($"Expected name and value separated by =. Got: '{line}'");
            var name = split[0];

            if (name == "Font")
            {
                var font = new FontFamily(split[1]);
                target["Font"] = font;
                continue;
            }

            var brush = HtmlToColor(split[1]);

            target[name] = brush;
        }
    }

    private static string ColorToHtml(Color color)
    {
        var sdColor = System.Drawing.Color.FromArgb(color.A, color.R, color.G, color.B);
        return ColorTranslator.ToHtml(sdColor);
    }

    private static Brush HtmlToColor(string colorString)
    {
        var sdColor = ColorTranslator.FromHtml(colorString);
        var color = Color.FromArgb(sdColor.A, sdColor.R, sdColor.G, sdColor.B);
        return new SolidColorBrush(color);
    }
}
