using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using BSU.BSO.FileStructures;
using BSU.Hashes;
using Newtonsoft.Json;
using zsyncnet;

namespace BSU.Server;

public record ModUpdateOptions;
public static class ModUpdater
{
    private record FileHash(byte[] Hash, ulong FileSize);

    public static void UpdateMod(string name, ISourceMod source, IDestinationMod destination, ModUpdateOptions options)
    {
        Console.WriteLine($"Working on mod {name}");

        Console.WriteLine($"Hashing source files");
        var sourceHashes = HashSourceFiles(source);

        Console.WriteLine($"Hashing destination files");
        var destinationFiles = destination.GetFileList();
        var commonFiles = sourceHashes.Keys.Intersect(destinationFiles).ToList();
        var destinationHashes = GetDestinationHashes(destination, commonFiles, destinationFiles);

        var deletes = FindDeletes(sourceHashes.Keys, destinationFiles);
        var updates = FindUpdates(sourceHashes, destinationHashes);

        Console.WriteLine($"{deletes.Count} deletes");
        Console.WriteLine($"{updates.Count} updates");

        foreach (var path in deletes)
        {
            destination.Remove(path);
        }

        DoUpdates(source, destination, updates, sourceHashes, destinationFiles, options);

        var oldHashFile = TryReadHashFile(destination, destinationFiles);
        var hashFile = BuildHashFile(name, sourceHashes);
        if (oldHashFile != null && HashFilesMatch(oldHashFile, hashFile))
            return;
        var hashFileJson = JsonConvert.SerializeObject(hashFile);
        var hashJsonStream = Util.StringToStream(hashFileJson);
        destination.Write("/hash.json", hashJsonStream);
    }

    private static bool HashFilesMatch(HashFile a, HashFile b)
    {
        if (a.FolderName != b.FolderName) return false;
        if (a.Hashes.Count != b.Hashes.Count) return false;
        var aSorted = a.Hashes.OrderBy(h => h.FileName).ToList();
        var bSorted = b.Hashes.OrderBy(h => h.FileName).ToList();
        for (int i = 0; i < aSorted.Count; i++)
        {
            var aHash = aSorted[i];
            var bHash = bSorted[i];
            if (aHash.FileName != bHash.FileName) return false;
            if (aHash.FileSize != bHash.FileSize) return false;
            if (!aHash.Hash.SequenceEqual(bHash.Hash)) return false;
        }

        return true;
    }

    private static HashFile? TryReadHashFile(IDestinationMod destination, List<NormalizedPath> destinationFiles)
    {
        if (!destinationFiles.Contains("/hash.json"))
            return null;
        using var stream = destination.OpenRead("/hash.json");
        var reader = new StreamReader(stream);
        var json = reader.ReadToEnd();
        return JsonConvert.DeserializeObject<HashFile>(json);
    }

    private static void DoUpdates(ISourceMod source, IDestinationMod destination, List<NormalizedPath> updates, Dictionary<NormalizedPath, FileHash> sourceHashes,
        List<NormalizedPath> destinationFiles, ModUpdateOptions modUpdateOptions)
    {
        // TODO: pipeline, so that we always have 1GB (adjustable) prepared for upload.
        // -> prepare-worker and upload-worker. If deletes or small file uploads are super slow, use more to mask that. but need to make sure they handle different files

        foreach (var path in updates)
        {
            Console.WriteLine($"Updating {path}");

            var hashPath = new NormalizedPath(path + ".hash");

            if (destinationFiles.Contains(hashPath))
                destination.Remove(hashPath);

            var fileInMemory = new MemoryStream();
            using (var sourceFile = source.OpenRead(path))
            {
                sourceFile.CopyTo(fileInMemory);
            }

            fileInMemory.Position = 0;
            destination.Write(path, fileInMemory);

            fileInMemory.Position = 0;
            var cfStream = BuildControlFileStream(fileInMemory, source.GetLastChangeDateTime(path), path.GetFileName());
            destination.Write(path + ".zsync", cfStream);

            // TODO: zip, if wanted

            destination.Write(hashPath, new MemoryStream(sourceHashes[path].Hash));
        }
    }

    private static Stream BuildControlFileStream(Stream file, DateTime lastWrite, string fileName)
    {
        var controlFile =
            ZsyncMake.MakeControlFile(file, lastWrite, fileName);
        var cfStream = new MemoryStream();
        controlFile.WriteToStream(cfStream);
        cfStream.Position = 0;
        return cfStream;
    }

    private static List<NormalizedPath> FindDeletes(IEnumerable<NormalizedPath> sourceFiles,
        IEnumerable<NormalizedPath> destinationFiles)
    {
        var unaccountedFiles = new List<NormalizedPath>(destinationFiles);

        unaccountedFiles.Remove("/hash.json");

        var suffixes = new[] { "", ".zsync", ".zip", ".hash" };

        foreach (var sourcePath in sourceFiles)
        {
            foreach (var suffix in suffixes)
            {
                unaccountedFiles.Remove(sourcePath + suffix);
            }
        }

        return unaccountedFiles;
    }

    private static List<NormalizedPath> FindUpdates(Dictionary<NormalizedPath, FileHash> sourceHashes,
        Dictionary<NormalizedPath, byte[]> destinationHashes)
    {
        var updates = new List<NormalizedPath>();

        foreach (var (sourcePath, sourceHash) in sourceHashes)
        {
            if (!destinationHashes.TryGetValue(sourcePath, out var destinationHash) ||
                !sourceHash.Hash.SequenceEqual(destinationHash))
                updates.Add(sourcePath);
        }

        return updates;
    }

    private static Dictionary<NormalizedPath, byte[]> GetDestinationHashes(IDestinationMod destination, List<NormalizedPath> fileList, List<NormalizedPath> destinationFileList)
    {
        var result = new Dictionary<NormalizedPath, byte[]>();

        foreach (var path in fileList)
        {
            var hashPath = new NormalizedPath(path + ".hash");
            if (!destinationFileList.Contains(hashPath))
                continue;
            var stream = destination.OpenRead(hashPath);
            var hash = new byte[stream.Length];
            var read = stream.Read(hash);
            if (read != hash.Length)
                throw new NotImplementedException();
            result.Add(path, hash);
        }

        return result;
    }

    private static HashFile BuildHashFile(string modName, Dictionary<NormalizedPath, FileHash> hashes)
    {
        var bsoHashes = new List<BsoFileHash>();

        foreach (var (path, (hash, fileSize)) in hashes)
        {
            bsoHashes.Add(new BsoFileHash(path, hash, fileSize));
        }

        return new HashFile(modName, bsoHashes);
    }

    private static Dictionary<NormalizedPath, FileHash> HashSourceFiles(ISourceMod source)
    {
        var hashes = new Dictionary<NormalizedPath, FileHash>();
        foreach (var path in source.GetFileList().OrderBy(file => file))
        {
            using var fileStream = source.OpenRead(path);
            var length = fileStream.Length;
            var hash = Sha1AndPboHash.BuildAsync(fileStream, path.GetExtension(), CancellationToken.None).Result;
            hashes[path] = new FileHash(hash.GetBytes(), (ulong)length);
        }

        return hashes;
    }
}
