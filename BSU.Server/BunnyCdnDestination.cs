using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Reflection;
using BunnyCDN.Net.Storage;

namespace BSU.Server;

public class BunnyCdnDestination : IDestinationMod
{
    private readonly string _basePath;
    private readonly string _modName;
    private readonly bool _dryRun;
    private readonly ChangedFileTracker _changedFileTracker;
    private readonly BunnyCDNStorage _storage;

    public BunnyCdnDestination(BunnyCdnConfig config, string modName, bool dryRun,
        ChangedFileTracker changedFileTracker)
    {
        _basePath = $"/{config.ZoneName}/{modName}";
        _modName = modName;
        _dryRun = dryRun;
        _changedFileTracker = changedFileTracker;
        _storage = new BunnyCDNStorage(config.ZoneName, config.ApiKey, config.Region);
        var httpClientField = _storage.GetType().GetField("_http", BindingFlags.NonPublic | BindingFlags.Instance);
        var httpClient = (HttpClient)httpClientField!.GetValue(_storage)!;
        httpClient.Timeout = new TimeSpan(1, 0, 0);
    }

    public Dictionary<NormalizedPath, long> GetFileList()
    {
        var result = new Dictionary<NormalizedPath, long>();

        var foldersToCheck = new Queue<string>();
        foldersToCheck.Enqueue(_basePath);
        while (foldersToCheck.TryDequeue(out var folder))
        {
            var objects = _storage.GetStorageObjectsAsync(folder + "/").GetAwaiter().GetResult();
            foreach (var storageObject in objects)
            {
                if (storageObject.IsDirectory)
                {
                    var path = storageObject.FullPath;
                    foldersToCheck.Enqueue(path);
                }
                else
                {
                    result.Add(GetRelativePath(storageObject.FullPath), storageObject.Length);
                }
            }
        }

        return result;
    }

    private NormalizedPath GetRelativePath(string storagePath)
    {
        if (!storagePath.StartsWith(_basePath))
            throw new ArgumentException();
        return storagePath[_basePath.Length..];
    }

    private string GetStoragePath(NormalizedPath relativePath)
    {
        return _basePath + relativePath;
    }

    public Stream OpenRead(NormalizedPath path)
    {
        var storagePath = GetStoragePath(path);
        return _storage.DownloadObjectAsStreamAsync(storagePath).GetAwaiter().GetResult();
    }

    public void Write(NormalizedPath path, Stream data)
    {
        if (_dryRun)
        {
            Console.WriteLine($"Would write {path}");
            return;
        }

        _changedFileTracker.AddChangedFilePath(_modName, path);
        var storagePath = GetStoragePath(path);
        _storage.UploadAsync(data, storagePath, true).GetAwaiter().GetResult();
    }

    public void Remove(NormalizedPath path)
    {
        if (_dryRun)
        {
            Console.WriteLine($"Would remove {path}");
            return;
        }

        var storagePath = GetStoragePath(path);
        _storage.DeleteObjectAsync(storagePath).GetAwaiter().GetResult();
    }
}
