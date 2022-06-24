using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BSU.BSO.FileStructures;
using BSU.Hashes;
using Newtonsoft.Json;
using zsyncnet;

namespace BSU.Server;

public static class ModUpdater
{
    private record FileHash(byte[] Hash, ulong FileSize);

    public static (ModUpdateStats stats, ModFolder modInfo) UpdateMod(string name, ISourceMod source, IDestinationMod destination)
    {
        var stats = new ModUpdateStats(name);

        Console.WriteLine($"Working on mod {name}");

        Console.WriteLine("Hashing source files");
        var sourceHashes = HashSourceFiles(source);

        Console.WriteLine("Listing destination files");
        var destinationFiles = destination.GetFileList();
        var commonFiles = sourceHashes.Keys.Intersect(destinationFiles.Keys).ToList();

        Console.WriteLine("Retrieving destination hashes");
        var destinationHashes = GetDestinationHashes(destination, commonFiles, destinationFiles);

        var deletes = FindDeletes(sourceHashes.Keys, destinationFiles.Keys, stats);
        var updates = FindUpdates(sourceHashes, destinationHashes, stats);

        Console.WriteLine($"{deletes.Count} deletes");
        Console.WriteLine($"{updates.Count} updates");

        foreach (var path in deletes)
        {
            destination.Remove(path);
        }

        DoUpdates(source, destination, updates, sourceHashes, destinationFiles);

        var oldHashFile = TryReadHashFile(destination, destinationFiles.Keys);
        var hashFile = BuildHashFile(name, sourceHashes);
        var modInfo = new ModFolder(name, hashFile.BuildModHash());
        if (oldHashFile != null && HashFilesMatch(oldHashFile, hashFile))
            return (stats, modInfo);
        var hashFileJson = JsonConvert.SerializeObject(hashFile);
        var hashJsonStream = Util.StringToStream(hashFileJson);
        destination.Write("/hash.json", hashJsonStream);
        return (stats, modInfo);
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

    private static HashFile? TryReadHashFile(IDestinationMod destination, ICollection<NormalizedPath> destinationFiles)
    {
        if (!destinationFiles.Contains("/hash.json"))
            return null;
        using var stream = destination.OpenRead("/hash.json");
        var reader = new StreamReader(stream);
        var json = reader.ReadToEnd();
        return JsonConvert.DeserializeObject<HashFile>(json);
    }

    private record UpdateWorkerInfo(ISourceMod Source, IDestinationMod Destination, ConcurrentQueue<NormalizedPath> Updates,
        Dictionary<NormalizedPath, FileHash> SourceHashes,
        Dictionary<NormalizedPath, long> DestinationFiles);

    private static void DoUpdates(ISourceMod source, IDestinationMod destination, List<NormalizedPath> updates,
        Dictionary<NormalizedPath, FileHash> sourceHashes,
        Dictionary<NormalizedPath, long> destinationFiles)
    {
        var queue = new ConcurrentQueue<NormalizedPath>(updates);

        var workerInfo = new UpdateWorkerInfo(source, destination, queue, sourceHashes, destinationFiles);

        var workers = new List<Thread>();

        for (int i = 0; i < 4; i++)
        {
            var worker = new Thread(UpdateWorker);
            workers.Add(worker);
            worker.Start(workerInfo);
        }

        foreach (var worker in workers)
        {
            worker.Join();
        }
    }

    private static void UpdateWorker(object? workerInfoObj)
    {
        var workerInfo = (UpdateWorkerInfo)workerInfoObj!;

        var (source, destination, queue, sourceHashes, destinationFiles) = workerInfo;

        while (queue.TryDequeue(out var path))
        {
            Console.WriteLine($"Updating {path}");

            var hashPath = new NormalizedPath(path + ".hash");

            if (destinationFiles.ContainsKey(hashPath))
                destination.Remove(hashPath);

            byte[] fileInMemory;
            using (var sourceFile = source.OpenRead(path))
            {
                fileInMemory = new byte[sourceFile.Length];
                var read = sourceFile.Read(fileInMemory);
                if (read != sourceFile.Length)
                    throw new NotImplementedException();
            }

            destination.Write(path, new MemoryStream(fileInMemory));

            var cfStream = BuildControlFileStream(new MemoryStream(fileInMemory), source.GetLastChangeDateTime(path), path.GetFileName());
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
        IEnumerable<NormalizedPath> destinationFiles, ModUpdateStats stats)
    {
        var unaccountedFiles = new List<NormalizedPath>(destinationFiles);

        unaccountedFiles.Remove("/hash.json");

        var suffixes = new[] { "", ".zsync", ".hash" };

        foreach (var sourcePath in sourceFiles)
        {
            foreach (var suffix in suffixes)
            {
                unaccountedFiles.Remove(sourcePath + suffix);
            }
        }

        stats.Deleted = unaccountedFiles.Count(f =>
        {
            var ext = f.GetExtension();
            return ext != "zsync" && ext != "hash";
        });
        return unaccountedFiles;
    }

    private static List<NormalizedPath> FindUpdates(Dictionary<NormalizedPath, FileHash> sourceHashes,
        Dictionary<NormalizedPath, byte[]> destinationHashes, ModUpdateStats stats)
    {
        var updates = new List<NormalizedPath>();

        foreach (var (sourcePath, sourceHash) in sourceHashes)
        {
            if (!destinationHashes.TryGetValue(sourcePath, out var destinationHash))
            {
                stats.New++;
                updates.Add(sourcePath);
                continue;
            }

            if (!sourceHash.Hash.SequenceEqual(destinationHash))
            {
                stats.Updated++;
                updates.Add(sourcePath);
            }
        }

        return updates;
    }

    private static Dictionary<NormalizedPath, byte[]> GetDestinationHashes(IDestinationMod destination, List<NormalizedPath> fileList, Dictionary<NormalizedPath, long> destinationFileList)
    {
        var result = new ConcurrentDictionary<NormalizedPath, byte[]>();

        var options = new ParallelOptions { MaxDegreeOfParallelism = 5 };

        Parallel.ForEach(fileList, options, path =>
        {
            var hashPath = new NormalizedPath(path + ".hash");
            if (!destinationFileList.TryGetValue(hashPath, out var hashLength))
                return;
            using var stream = destination.OpenRead(hashPath);
            var hash = new byte[hashLength];
            var read = stream.Read(hash);
            if (read != hash.Length)
                throw new NotImplementedException();
            result.TryAdd(path, hash);
        });

        return result.ToDictionary(kv => kv.Key, kv => kv.Value);
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
