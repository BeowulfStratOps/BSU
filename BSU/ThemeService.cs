using System.Collections.Generic;
using System.IO;
using System.Linq;
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

        // TODO: copy built-in themes
        var darkPath = Path.Combine(_themeFolder.FullName, "Dark.theme");
        File.Copy(lightPath, darkPath, true);
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
        var files = _themeFolder.EnumerateFiles($"*.theme");
        return files.Select(fi => fi.Name).ToList();
    }

    public string GetDefaultTheme()
    {
        var useLightTheme = (int?)Registry.CurrentUser
            .OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Themes\\Personalize")
            ?.GetValue("AppsUseLightTheme") ?? 1;
        return useLightTheme == 1 ? "Light" : "Dark";
    }
}
