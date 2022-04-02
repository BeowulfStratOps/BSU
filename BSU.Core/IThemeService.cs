using System.Collections.Generic;

namespace BSU.Core;

public interface IThemeService
{
    void SetTheme(string theme);
    List<string> GetAvailableThemes();
    string GetDefaultTheme();
}
