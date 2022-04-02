using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using BSU.Core;
using Microsoft.Win32;

namespace BSU.GUI;

public class ThemeService : IThemeService
{
    private readonly ResourceDictionary _resources;
    private readonly DirectoryInfo _themeFolder;

    private string? _currentTheme;

    public ThemeService(ResourceDictionary resources, string themeFolder)
    {
        _resources = resources;
        _themeFolder = new DirectoryInfo(themeFolder);
        if (!_themeFolder.Exists)
            _themeFolder.Create();
        CreateOrUpdateSystemThemes();
    }

    private void CreateOrUpdateSystemThemes()
    {
        // TODO: make sure we don't overwrite user-created themes

        var lightPath = Path.Combine(_themeFolder.FullName, "Light.theme");
        ThemeFile.CreateDefaultThemeFile(lightPath);

        CopyBuiltInTheme("Dark");
    }

    private void CopyBuiltInTheme(string name)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceStream = assembly.GetManifestResourceStream($"BSU.GUI.Themes.{name}.theme")!;
        var themePath = Path.Combine(_themeFolder.FullName, $"{name}.theme");

        using var file = new FileStream(themePath, FileMode.Create, FileAccess.Write);
        resourceStream.CopyTo(file);
    }

    public void SetTheme(string theme)
    {
        if (_currentTheme == theme) return;
        _currentTheme = theme;

        var path = Path.Combine(_themeFolder.FullName, $"{theme}.theme");
        ThemeFile.LoadTheme(path, _resources);
    }

    public List<string> GetAvailableThemes()
    {
        const string suffix = ".theme";
        var files = _themeFolder.EnumerateFiles($"*{suffix}");
        return files.Select(fi => fi.Name[..^suffix.Length]).ToList();
    }

    public string GetDefaultTheme()
    {
        var useLightTheme = (int?)Registry.CurrentUser
            .OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Themes\\Personalize")
            ?.GetValue("AppsUseLightTheme") ?? 1;
        return useLightTheme == 1 ? "Light" : "Dark";
    }
}
