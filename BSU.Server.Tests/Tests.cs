using System;
using Xunit;

namespace BSU.Server.Tests;

public class Tests
{
    private const int Mb = 1024 * 1024;

    [Fact]
    public void Empty()
    {
        var sourceMod = new MockSourceMod();
        var destinationMod = new MockDestinationMod();

        ModUpdater.UpdateMod("Test", sourceMod, destinationMod);

        TestUtil.AssertCorrect(sourceMod.Files, destinationMod.Files);
    }

    [Fact]
    public void TestNewFile()
    {
        var sourceMod = new MockSourceMod();
        sourceMod.Files.Add("/addons/test.pbo", TestUtil.RandomData(10 * Mb));
        var destinationMod = new MockDestinationMod();

        ModUpdater.UpdateMod("Test", sourceMod, destinationMod);

        TestUtil.AssertCorrect(sourceMod.Files, destinationMod.Files);
        TestUtil.CheckChangedFiles(new[]
        {
            "/hash.json",
            "/addons/test.pbo", "/addons/test.pbo.hash", "/addons/test.pbo.zsync"
        }, destinationMod.WrittenFiles);
    }

    [Fact]
    public void TestNoChange()
    {
        var testPboData = TestUtil.RandomData(10 * Mb);

        var sourceMod = new MockSourceMod();
        sourceMod.Files.Add("/addons/test.pbo", testPboData);
        var destinationMod = new MockDestinationMod();
        destinationMod.Files.Add("/addons/test.pbo", testPboData);
        destinationMod.Files.Add("/addons/test.pbo.hash", TestUtil.PboHash(testPboData));
        destinationMod.Files.Add("/addons/test.pbo.zsync", TestUtil.ControlFile(testPboData, "test.pbo"));

        ModUpdater.UpdateMod("Test", sourceMod, destinationMod);

        TestUtil.AssertCorrect(sourceMod.Files, destinationMod.Files);
        TestUtil.CheckChangedFiles(new[]
        {
            "/hash.json"
        }, destinationMod.WrittenFiles);
        TestUtil.CheckChangedFiles(Array.Empty<string>(), destinationMod.RemovedFiles);
    }

    [Fact]
    public void TestChangedFile()
    {
        var testPboData = TestUtil.RandomData(10 * Mb);
        var test2PboData = TestUtil.RandomData(10 * Mb);

        var sourceMod = new MockSourceMod();
        sourceMod.Files.Add("/addons/test.pbo", testPboData);
        var destinationMod = new MockDestinationMod();
        destinationMod.Files.Add("/addons/test.pbo", test2PboData);
        destinationMod.Files.Add("/addons/test.pbo.hash", TestUtil.PboHash(test2PboData));
        destinationMod.Files.Add("/addons/test.pbo.zsync", TestUtil.ControlFile(test2PboData, "test.pbo"));

        ModUpdater.UpdateMod("Test", sourceMod, destinationMod);

        TestUtil.AssertCorrect(sourceMod.Files, destinationMod.Files);
        TestUtil.CheckChangedFiles(new[]
        {
            "/hash.json",
            "/addons/test.pbo", "/addons/test.pbo.hash", "/addons/test.pbo.zsync",
        }, destinationMod.WrittenFiles);
        TestUtil.CheckChangedFiles(new[]
        {
            "/addons/test.pbo.hash",
        }, destinationMod.RemovedFiles);
    }

    [Fact]
    public void TestRemovedFile()
    {
        var testPboData = TestUtil.RandomData(10 * Mb);

        var sourceMod = new MockSourceMod();
        var destinationMod = new MockDestinationMod();
        destinationMod.Files.Add("/addons/test.pbo", testPboData);
        destinationMod.Files.Add("/addons/test.pbo.hash", TestUtil.PboHash(testPboData));
        destinationMod.Files.Add("/addons/test.pbo.zsync", TestUtil.ControlFile(testPboData, "test.pbo"));

        ModUpdater.UpdateMod("Test", sourceMod, destinationMod);

        TestUtil.AssertCorrect(sourceMod.Files, destinationMod.Files);
        TestUtil.CheckChangedFiles(new[]
        {
            "/hash.json"
        }, destinationMod.WrittenFiles);
        TestUtil.CheckChangedFiles(new[]
        {
            "/addons/test.pbo", "/addons/test.pbo.hash", "/addons/test.pbo.zsync"
        }, destinationMod.RemovedFiles);
    }

    [Fact]
    public void TestFileWithoutHash()
    {
        var testPboData = TestUtil.RandomData(10 * Mb);

        var sourceMod = new MockSourceMod();
        sourceMod.Files.Add("/addons/test.pbo", testPboData);
        var destinationMod = new MockDestinationMod();
        destinationMod.Files.Add("/addons/test.pbo", testPboData);
        destinationMod.Files.Add("/addons/test.pbo.zsync", TestUtil.ControlFile(testPboData, "test.pbo"));

        ModUpdater.UpdateMod("Test", sourceMod, destinationMod);

        TestUtil.AssertCorrect(sourceMod.Files, destinationMod.Files);
        TestUtil.CheckChangedFiles(new[]
        {
            "/hash.json",
            "/addons/test.pbo", "/addons/test.pbo.hash", "/addons/test.pbo.zsync"
        }, destinationMod.WrittenFiles);
        TestUtil.CheckChangedFiles(Array.Empty<string>(), destinationMod.RemovedFiles);
    }
    
    [Fact]
    public void TestModHashMatch()
    {
        var sourceMod1 = new MockSourceMod();
        sourceMod1.Files.Add("/addons/test.pbo", TestUtil.RandomData(10 * Mb, 1));
        var destinationMod1 = new MockDestinationMod();
        
        var sourceMod2 = new MockSourceMod();
        sourceMod2.Files.Add("/addons/test.pbo", TestUtil.RandomData(10 * Mb, 1));
        var destinationMod2 = new MockDestinationMod();

        var (_, info1) = ModUpdater.UpdateMod("Test", sourceMod1, destinationMod1);
        var (_, info2) = ModUpdater.UpdateMod("Test", sourceMod2, destinationMod2);

        Assert.Equal(info1.Hash, info2.Hash);
    }
    
    [Fact]
    public void TestModHashDiffer()
    {
        var sourceMod1 = new MockSourceMod();
        sourceMod1.Files.Add("/addons/test.pbo", TestUtil.RandomData(10 * Mb, 1));
        var destinationMod1 = new MockDestinationMod();
        
        var sourceMod2 = new MockSourceMod();
        sourceMod2.Files.Add("/addons/test.pbo", TestUtil.RandomData(10 * Mb, 2));
        var destinationMod2 = new MockDestinationMod();

        var (_, info1) = ModUpdater.UpdateMod("Test", sourceMod1, destinationMod1);
        var (_, info2) = ModUpdater.UpdateMod("Test", sourceMod2, destinationMod2);

        Assert.NotEqual(info1.Hash, info2.Hash);
    }

    // TODO: test combinations of the above (or just all of them)
    // TODO: test cancellation
    // TODO: test more failure stuff
}
