using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using BSU.BSO.FileStructures;
using Newtonsoft.Json;
using Xunit;
using zsyncnet;

namespace BSU.Server.Tests;

public static class TestUtil
{
    private static readonly Random Random = new Random();

    public static void AssertCorrect(IReadOnlyDictionary<NormalizedPath, byte[]> sourceFiles, IReadOnlyDictionary<NormalizedPath, byte[]> destinationFiles)
    {
        var expectedHashes = new List<BsoFileHash>();
        var accountedFiles = new List<NormalizedPath> { "/hash.json" };
        foreach (var (path, data) in sourceFiles)
        {
            Assert.Contains(path, destinationFiles);
            Assert.Equal(data, destinationFiles[path]);

            var hash = ((string)path).EndsWith(".pbo") ? data[^20..].ToArray() : GetSha1(data);

            expectedHashes.Add(new BsoFileHash(path, hash, (ulong)data.Length));

            var dotHashFile = destinationFiles[path + ".hash"];
            Assert.Equal(hash, dotHashFile);

            var checkZsync = new MemoryStream();
            var cfStream = new MemoryStream(destinationFiles[path + ".zsync"]);
            var controlFile = new ControlFile(cfStream);
            Zsync.Sync(controlFile, new List<Stream>(), new DummyRangeDownloader(destinationFiles[path]), checkZsync);
            Assert.Equal(data, checkZsync.ToArray());

            // TODO: check zip

            accountedFiles.Add(path);
            accountedFiles.Add(path + ".zsync");
            accountedFiles.Add(path + ".hash");
        }

        expectedHashes = expectedHashes.OrderBy(h => h.FileName).ToList();

        Assert.Contains("/hash.json", destinationFiles);
        var hashJson = Encoding.UTF8.GetString(destinationFiles["/hash.json"]);
        var hashFile = JsonConvert.DeserializeObject<HashFile>(hashJson);
        var writtenHashes = hashFile.Hashes.OrderBy(h => h.FileName).ToList();

        Assert.Equal(expectedHashes.Count, writtenHashes.Count);

        for (int i = 0; i < expectedHashes.Count; i++)
        {
            var expected = expectedHashes[i];
            var actual = writtenHashes[i];
            Assert.Equal(expected.FileName, actual.FileName);
            Assert.Equal(expected.Hash, actual.Hash);
            Assert.Equal(expected.FileSize, actual.FileSize);
        }

        Assert.Empty(destinationFiles.Keys.Except(accountedFiles));
    }

    private static byte[] GetSha1(byte[] data)
    {
        var sha1 = SHA1.Create();
        return sha1.ComputeHash(data);
    }

    public static byte[] RandomData(ulong length)
    {
        var buffer = new byte[length];
        Random.NextBytes(buffer);
        return buffer;
    }
    
    public static byte[] RandomData(ulong length, int seed)
    {
        var buffer = new byte[length];
        new Random(seed).NextBytes(buffer);
        return buffer;
    }

    public static void CheckChangedFiles(IEnumerable<string> strings, HashSet<NormalizedPath> destinationModWrittenFiles)
    {
        var expected = strings.OrderBy(p => p).ToList();
        var actual = destinationModWrittenFiles.Select(p => (string)p).OrderBy(p => p).ToList();
        Assert.Equal(expected, actual);
    }

    public static byte[] PboHash(byte[] testPboData) => testPboData[^20..];

    public static byte[] ControlFile(byte[] data, string name)
    {
        var memStream = new MemoryStream(data);
        var cf = ZsyncMake.MakeControlFile(memStream, DateTime.Now, "test");
        var output = new MemoryStream();
        cf.WriteToStream(output);
        return output.ToArray();
    }
}

public class DummyRangeDownloader : IRangeDownloader
{
    private readonly byte[] _file;

    public DummyRangeDownloader(byte[] file)
    {
        _file = file;
    }

    public Stream DownloadRange(long @from, long to)
    {
        return new MemoryStream(_file.AsSpan((int)@from, (int)(to - @from)).ToArray());
    }

    public Stream Download()
    {
        throw new NotImplementedException();
    }
}
