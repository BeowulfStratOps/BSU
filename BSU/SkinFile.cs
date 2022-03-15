using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using NLog;
using Brush = System.Windows.Media.Brush;
using Color = System.Windows.Media.Color;
using FontFamily = System.Windows.Media.FontFamily;

namespace BSU.GUI;

public class SkinFile : ResourceDictionary
{
    private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();

    public SkinFile()
    {
        try
        {
            if (TryFindSkinFile(out var skinPath))
                LoadSkin(skinPath);
            else
                CreateDefaultSkinFile(skinPath);
        }
        catch (Exception e)
        {
            Logger.Error(e);
            throw;
        }
    }

    private void CreateDefaultSkinFile(string skinPath)
    {
        var skinResourceDictionary = new ResourceDictionary
        {
            Source = new Uri(";component/Resources/Skin.xaml", UriKind.RelativeOrAbsolute)
        };

        var entries = new List<(string key, string value)>();

        // TODO: sort keys
        foreach (DictionaryEntry entry in skinResourceDictionary)
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

        using var writer = new StreamWriter(skinPath);
        foreach (var (key, value) in entries.OrderBy(e => e.key))
        {
            writer.WriteLine($"# {key}={value}");
        }
    }

    private static bool TryFindSkinFile(out string skinPath)
    {
        var assemblyLocation = Assembly.GetExecutingAssembly().Location;
        skinPath = Path.Combine(Directory.GetParent(assemblyLocation)!.Parent!.FullName, "default.skin");

        var exists = File.Exists(skinPath);

        Logger.Info($"Checking possible skin file location: {skinPath} -> Exists: {exists}");

        return exists;
    }

    private void LoadSkin(string skinPath)
    {
        var skinText = File.ReadAllText(skinPath);

        foreach (var line in skinText.Split("\n", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
        {
            if (line.StartsWith("#")) continue;
            var split = line.Split("=", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            if (split.Length != 2)
                throw new InvalidDataException($"Expected name and value separated by =. Got: '{line}'");
            var name = split[0];

            if (name == "Font")
            {
                var font = new FontFamily(split[1]);
                this["Font"] = font;
                continue;
            }

            var brush = HtmlToColor(split[1]);

            this[name] = brush;
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
