using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace BSU.Server;

public class LocalDestinationMod : IDestinationMod
{
    private readonly DirectoryInfo _destinationPath;
    private readonly bool _dryRun;

    public LocalDestinationMod(DirectoryInfo destinationPath, bool dryRun)
    {
        _destinationPath = destinationPath;
        _dryRun = dryRun;
    }

    public Dictionary<NormalizedPath, long> GetFileList()
    {
        var fis = _destinationPath.EnumerateFiles("*", SearchOption.AllDirectories);
        return fis.ToDictionary(fi => Util.GetRelativePath(_destinationPath, fi), fi => fi.Length);
    }

    public Stream OpenRead(NormalizedPath path)
    {
        var fullPath = Util.GetAbsolutePath(_destinationPath, path);
        var fs = new FileStream(fullPath, FileMode.Open, FileAccess.Read);
        return fs;
    }

    public void Write(NormalizedPath path, Stream data)
    {
        if (_dryRun)
        {
            Console.WriteLine($"Would write {path}");
            return;
        }
        var fullPath = Util.GetAbsolutePath(_destinationPath, path);
        var fileInfo = new FileInfo(fullPath);
        fileInfo.Directory!.Create();
        using var fs = new FileStream(fullPath, FileMode.Create, FileAccess.Write);
        data.CopyTo(fs);
    }

    public void Remove(NormalizedPath path)
    {
        if (_dryRun)
        {
            Console.WriteLine($"Would remove {path}");
            return;
        }
        var fullPath = Util.GetAbsolutePath(_destinationPath, path);
        if (!File.Exists(fullPath))
            throw new FileNotFoundException(fullPath); // This would imply an issue with the algorithm
        File.Delete(fullPath);
    }
}
