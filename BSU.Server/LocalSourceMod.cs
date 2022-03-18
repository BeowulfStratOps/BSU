using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace BSU.Server;

public class LocalSourceMod : ISourceMod
{
    private readonly DirectoryInfo _sourcePath;

    public LocalSourceMod(DirectoryInfo sourcePath)
    {
        _sourcePath = sourcePath;
    }

    public List<NormalizedPath> GetFileList()
    {
        var fis = _sourcePath.EnumerateFiles("*", SearchOption.AllDirectories);
        return fis.Select(fi => Util.GetRelativePath(_sourcePath, fi)).ToList();
    }

    public Stream OpenRead(NormalizedPath path)
    {
        var fullPath = Util.GetAbsolutePath(_sourcePath, path);
        return new FileStream(fullPath, FileMode.Open, FileAccess.Read);
    }

    public DateTime GetLastChangeDateTime(NormalizedPath path)
    {
        var fullPath = Util.GetAbsolutePath(_sourcePath, path);
        return File.GetLastWriteTime(fullPath);
    }
}
