using System.Collections.Generic;
using System.Text;
using System.Threading;
using BSU.CoreCommon;
using BSU.CoreCommon.Hashes;
using BSU.Hashes;
using Moq;
using Newtonsoft.Json;
using Xunit;

namespace BSU.Core.Tests;

public class SerializationTests
{
    private static JsonSerializerSettings JsonSettings => new()
    {
        TypeNameHandling = TypeNameHandling.Auto
    };
    
    [Fact]
    private void VersionHashSerializationTest()
    {
        var repoMod = new Mock<IRepositoryMod>(MockBehavior.Strict);
        repoMod.Setup(m => m.GetFileList(It.IsAny<CancellationToken>())).ReturnsAsync(new List<string>
        {
            "/mod.cpp",
            "/addons/test.pbo",
            "/keys/test.bikey"
        });
        repoMod.Setup(m => m.GetFileHash(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TestFileHash());

        var versionHash = VersionHash.CreateAsync(repoMod.Object, CancellationToken.None).Result;

        var container = new HashContainer { Hashes = new List<IModHash> { versionHash } };

        var serialized = JsonConvert.SerializeObject(container, JsonSettings);

        var deserialized = JsonConvert.DeserializeObject<HashContainer>(serialized, JsonSettings)!;
        
        Assert.True(container.Hashes[0].IsMatch(deserialized.Hashes[0]));
    }
    
    [Fact]
    private void MatchHashSerializationTest()
    {
        var repoMod = new Mock<IRepositoryMod>(MockBehavior.Strict);
        repoMod.Setup(m => m.GetFile("/mod.cpp", It.IsAny<CancellationToken>())).ReturnsAsync(Encoding.UTF8.GetBytes("name = \"TestMod\";"));
        repoMod.Setup(m => m.GetFileList(It.IsAny<CancellationToken>())).ReturnsAsync(new List<string>
        {
            "/mod.cpp",
            "/addons/test.pbo",
            "/keys/test.bikey"
        });

        var versionHash = MatchHash.CreateAsync(repoMod.Object, CancellationToken.None).Result;

        var container = new HashContainer { Hashes = new List<IModHash> { versionHash } };

        var serialized = JsonConvert.SerializeObject(container, JsonSettings);

        var deserialized = JsonConvert.DeserializeObject<HashContainer>(serialized, JsonSettings)!;
        
        Assert.True(container.Hashes[0].IsMatch(deserialized.Hashes[0]));
    }

    private class HashContainer
    {
        public List<IModHash> Hashes { get; init; } = new();
    }

    private class TestFileHash : FileHash
    {
        public override byte[] GetBytes() => new byte[] { 1, 2, 3, 4, 13, 37, 123, 45, 6 };
    }
}
