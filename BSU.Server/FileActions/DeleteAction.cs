using System;
using System.IO;

namespace BSU.Server.FileActions;

internal class DeleteAction : FileAction
{
    public DeleteAction(string relPath) : base(relPath)
    {
    }

    public override void Do(DirectoryInfo sourceDirectory, DirectoryInfo destinationDirectory, bool dryRun)
    {
        var destinationPath = Path.Combine(destinationDirectory.FullName, RelPath);

        if (dryRun)
        {
            Console.WriteLine($"Would delete file {destinationPath}");
            return;
        }

        Console.WriteLine($"Delecting file {destinationPath}");

        File.Delete(destinationPath);
    }
}
