using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows;
using NLog;
using Color = System.Windows.Media.Color;

namespace BSU.GUI;

public class SkinFile : ResourceDictionary
{
    private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();
    public SkinFile()
    {
        try
        {
            var skinPath = FindSkinFile();
            if (skinPath == null) return;
            LoadSkin(skinPath);
        }
        catch (Exception e)
        {
            Logger.Error(e);
            throw;
        }
    }

    private static string? FindSkinFile()
    {
        var assemblyLocation = Assembly.GetExecutingAssembly().Location;
        var skinPath = Path.Combine(Directory.GetParent(assemblyLocation)!.Parent!.FullName, "default.skin");

        var exists = File.Exists(skinPath);

        Logger.Info($"Checking possible skin file location: {skinPath} -> Exists: {exists}");

        return exists ? skinPath : null;
    }

    private void LoadSkin(string skinPath)
    {
        var skinText = File.ReadAllText(skinPath);

        foreach (var line in skinText.Split("\n", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
        {
            if (line.StartsWith("#")) continue;
            var split = line.Split(" ", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            if (split.Length != 2)
                throw new InvalidDataException($"Expected color name and value separated by space. Got: '{line}'");
            var name = split[0];
            var color = ParseColor(split[1]);

            this[name] = color;
        }
    }

    private static Color ParseColor(string color)
    {
        var sdColor = ColorTranslator.FromHtml(color);
        return Color.FromArgb(sdColor.A, sdColor.R, sdColor.G, sdColor.B);
    }
}
