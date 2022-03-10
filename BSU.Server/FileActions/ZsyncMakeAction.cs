using System;
using System.IO;
using zsyncnet;

namespace BSU.Server.FileActions;

internal class ZsyncMakeAction : FileAction
{
    public ZsyncMakeAction(string relPath) : base(relPath)
    {
    }

    public override void Do(DirectoryInfo sourceDirectory, DirectoryInfo destinationDirectory, bool dryRun)
    {
        var sourcePath = Path.Combine(sourceDirectory.FullName, RelPath);
        var destinationPath = Path.Combine(destinationDirectory.FullName, RelPath) + ".zsync";

        if (dryRun)
        {
            Console.WriteLine($"Would create zsync control file for {sourcePath} in {destinationPath}");
            return;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);

        Console.WriteLine($"Creating zsync control file for {sourcePath} in {destinationPath}");

        var controlFile = ZsyncMake.MakeControlFile(new FileInfo(sourcePath));
        controlFile.WriteToFile(destinationPath);
    }
}
