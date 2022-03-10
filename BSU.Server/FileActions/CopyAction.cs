using System;
using System.IO;

namespace BSU.Server.FileActions;

internal class CopyAction : FileAction
{
    public CopyAction(string relPath) : base(relPath)
    {
    }

    public override void Do(DirectoryInfo sourceDirectory, DirectoryInfo destinationDirectory, bool dryRun)
    {
        var sourcePath = Path.Combine(sourceDirectory.FullName, RelPath);
        var destinationPath = Path.Combine(destinationDirectory.FullName, RelPath);

        if (dryRun)
        {
            Console.WriteLine($"Would copy file {sourcePath} to {destinationPath}");
            return;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);

        Console.WriteLine($"Copying file {sourcePath} to {destinationPath}");

        File.Copy(sourcePath, destinationPath, true);
    }
}
