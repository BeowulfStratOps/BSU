using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BSU.BSO.FileStructures;
using BSU.Hashes;
using BSU.Server.FileActions;
using Newtonsoft.Json;

namespace BSU.Server;

public static class ModUpdater
{
    private record FileHash(byte[] Hash, ulong FileSize);

    public static void UpdateMod(DirectoryInfo source, DirectoryInfo destination, bool dryRun, int? zsyncThreads)
    {
        Console.WriteLine($"Working on mod {source.Name}");

        Console.WriteLine($"Hashing {source}");
        var sourceHashes = HashSourceFiles(source);

        Console.WriteLine($"Hashing existing files");
        var fileActions = CheckDestinationFiles(sourceHashes, destination);

        Console.WriteLine($"{fileActions.OfType<DeleteAction>().Count()} files to delete");
        Console.WriteLine($"{fileActions.OfType<CopyAction>().Count()} files to copy");
        Console.WriteLine($"{fileActions.OfType<ZsyncMakeAction>().Count()} files to zsync-make");

        var startCopyDelete = DateTime.Now;
        // Do deletes first to make sure that zsync files can never be out of sync
        // also to free up space first
        foreach (var action in fileActions.OfType<DeleteAction>())
        {
            action.Do(source, destination, dryRun);
        }

        foreach (var action in fileActions.OfType<CopyAction>())
        {
            action.Do(source, destination, dryRun);
        }
        var copyDeleteTime = DateTime.Now - startCopyDelete;

        var startZsync = DateTime.Now;
        var options = new ParallelOptions();
        if (zsyncThreads != null) options.MaxDegreeOfParallelism = (int)zsyncThreads;
        Parallel.ForEach(
            fileActions.OfType<ZsyncMakeAction>(),
            options,
            action => action.Do(source, destination, dryRun));
        var zsyncTime = DateTime.Now - startZsync;

        Console.WriteLine($"Copy/Delete took {copyDeleteTime.TotalSeconds:F1}s");
        Console.WriteLine($"Zsync make took {zsyncTime.TotalSeconds:F1}s");

        var hashFile = BuildHashFile(source.Name, sourceHashes);
        var hashFileJson = JsonConvert.SerializeObject(hashFile);
        var hashFilePath = Path.Combine(destination.FullName, "hash.json");
        if (dryRun)
            Console.WriteLine($"Would write file {hashFilePath}");
        else
            File.WriteAllText(hashFilePath, hashFileJson);
    }

    private static HashFile BuildHashFile(string modName, Dictionary<string, FileHash> hashes)
    {
        var bsoHashes = new List<BsoFileHash>();

        foreach (var (path, (hash, fileSize)) in hashes)
        {
            bsoHashes.Add(new BsoFileHash(path, hash, fileSize));
        }

        return new HashFile(modName, bsoHashes);
    }

    private static List<FileAction> CheckDestinationFiles(Dictionary<string, FileHash> sourceHashes, DirectoryInfo destination)
    {
        var actions = new List<FileAction>();

        List<string> existingFiles;
        if (destination.Exists)
        {
            existingFiles = destination.EnumerateFiles("*", SearchOption.AllDirectories)
                .OrderBy(fi => fi.FullName)
                .Select(f => GetNormalizedPath(destination.FullName, f.FullName))
                .ToList();
        }
        else
            // happens when dry running
            existingFiles = new List<string>();

        foreach (var (path, (hash, _)) in sourceHashes)
        {
            var destinationPath = Path.Combine(destination.FullName, path);
            var destinationPathZsync = destinationPath + ".zsync";

            var rebuildZsync = false;

            if (!File.Exists(destinationPath) || !CheckFileHashMatches(hash, destinationPath))
            {
                // delete the zsync file before we copy the actual file, so that they can never be out of sync, if the copy aborts for some reason
                if (File.Exists(destinationPathZsync))
                    actions.Add(new DeleteAction(path + ".zsync"));
                actions.Add(new CopyAction(path));
                rebuildZsync = true;
            }

            if (rebuildZsync || !File.Exists(destinationPathZsync))
                actions.Add(new ZsyncMakeAction(path));

            // both are now accounted for
            existingFiles.Remove(path);
            existingFiles.Remove(path + ".zsync");
        }

        actions.AddRange(existingFiles.Select(existingPath => new DeleteAction(existingPath)));

        return actions;
    }

    private static bool CheckFileHashMatches(byte[] hash, string destinationPath)
    {
        using var fileStream = File.OpenRead(destinationPath);
        var extension = destinationPath.Split(".")[^1];
        var fileHash = Sha1AndPboHash.BuildAsync(fileStream, extension, CancellationToken.None).Result;
        return fileHash.GetBytes().SequenceEqual(hash);
    }



    private static string GetNormalizedPath(string baseDirectory, string filePath)
    {
        return Path.GetRelativePath(baseDirectory, filePath).Replace('\\', '/');
    }

    private static Dictionary<string, FileHash> HashSourceFiles(DirectoryInfo modDirectory)
    {
        var hashes = new Dictionary<string, FileHash>();
        foreach (var file in modDirectory.EnumerateFiles("*", SearchOption.AllDirectories).OrderBy(fi => fi.FullName))
        {
            if (file.Extension.ToLowerInvariant() == ".zsync") continue;

            var length = file.Length;
            using var fileStream = file.OpenRead();
            var hash = Sha1AndPboHash.BuildAsync(fileStream, file.Name.Split(".")[^1], CancellationToken.None).Result;
            var relPath = GetNormalizedPath(modDirectory.FullName, file.FullName);
            hashes[relPath] = new FileHash(hash.GetBytes(), (ulong)length);
        }

        return hashes;
    }
}
