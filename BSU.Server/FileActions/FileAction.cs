using System.IO;

namespace BSU.Server.FileActions;

internal abstract class FileAction
{
    protected readonly string RelPath;

    protected FileAction(string relPath)
    {
        RelPath = relPath;
    }

    public abstract void Do(DirectoryInfo sourceDirectory, DirectoryInfo destinationDirectory, bool dryRun);
}
