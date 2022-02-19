using System;
using System.Threading;
using System.Threading.Tasks;
using BSU.Core.Hashes;
using BSU.Core.Tests.Mocks;
using Xunit;

namespace BSU.Core.Tests
{
    public class MatchHashTests
    {
        [Fact]
        private async Task Simple1()
        {
            Assert.True(await Check(Array.Empty<string>(), null, Array.Empty<string>(), null));
        }

        [Fact]
        private async Task Simple2()
        {
            Assert.True(await Check(Array.Empty<string>(), "qw123", Array.Empty<string>(), "qw123"));
        }

        [Fact]
        private async Task Simple3()
        {
            Assert.True(await Check(new[] {"/addons/qwe.pbo"}, null, new[] {"/addons/qwe.pbo"}, null));
        }

        [Fact]
        private async Task Simple4()
        {
            Assert.True(await Check(new[] {"/addons/qwe.pbo"}, "qw123", new[] {"/addons/qwe.pbo"}, "qw123"));
        }

        [Fact]
        private async Task True1()
        {
            Assert.True(await Check(new[] {"/addons/qwe.pbo", "/addons/asdf.pbo", "/addons/xyz.pbo"}, "gg",
                new[] {"/addons/qwe.pbo", "/addons/asdf.pbo", "/addons/abc.pbo"}, "gg"));
        }

        [Fact]
        private async Task True2()
        {
            Assert.True(await Check(new[] {"/addons/qwe.pbo", "/addons/asdf.pbo", "/addons/xyz.pbo"}, "gg",
                new[] {"/addons/qwe_f.pbo", "/addons/asdf_f.pbo", "/addons/abc.pbo"}, "gg"));
        }

        [Fact]
        private async Task False3()
        {
            Assert.False(await Check(new[] {"/addons/qwe.pbo", "/addons/asdf.pbo", "/addons/xyz.pbo"}, null,
                new[] {"/addons/qwe.pbo", "/addons/asdf.pbo", "/addons/abc.pbo"}, null));
        }

        [Fact]
        private async Task False0()
        {
            Assert.False(await Check(new[] {"/addons/qwe.pbo", "/addons/asdf.pbo", "/addons/xyz.pbo"}, "gl",
                new[] {"/addons/qwe.pbo", "/addons/asdf.pbo", "/addons/xyz.pbo"}, "hf"));
        }

        [Fact]
        private async Task False1()
        {
            Assert.False(await Check(new[] {"/addons/qwe1.pbo", "/addons/asdf1.pbo", "/addons/xyz.pbo"}, null,
                new[] {"/addons/qwe2.pbo", "/addons/asdf2.pbo", "/addons/abc.pbo"}, null));
        }

        [Fact]
        private async Task False2()
        {
            Assert.False(await Check(new[] {"/addons/qwe.pbo.bisign", "/addons/asdf.pbo.bisign", "/addons/xyz.pbo"}, null,
                new[] {"/addons/qwe.pbo.bisign", "/addons/asdf.pbo.bisign", "/addons/abc.pbo"}, null));
        }

        // TODO: test keys

        private static void AddFiles(IMockedFiles mod, string[] names, string? name)
        {
            foreach (var fileName in names)
            {
                mod.SetFile(fileName, "");
            }

            if (name != null) mod.SetFile("/mod.cpp", "name=\"" + name.Replace("\"", "\\\"") + "\";");
        }

        private static async Task<MatchHash> CreateStorageMod(string[] names, string? name)
        {
            var storageMod = new MockStorageMod(Task.CompletedTask);
            AddFiles(storageMod, names, name);
            return await MatchHash.CreateAsync(storageMod, CancellationToken.None);
        }

        private static async Task<MatchHash> CreateRepoMod(string[] names, string? name)
        {
            var repoMod = new MockRepositoryMod();
            AddFiles(repoMod, names, name);
            return await MatchHash.CreateAsync(repoMod, CancellationToken.None);
        }

        private static async Task<bool> Check(string[] fileNames1, string? modName1, string[] fileNames2, string? modName2)
        {
            var res1 = (await CreateStorageMod(fileNames1, modName1)).IsMatch(await CreateRepoMod(fileNames2, modName2));
            var res2 = (await CreateRepoMod(fileNames1, modName1)).IsMatch(await CreateStorageMod(fileNames2, modName2));
            Assert.Equal(res1, res2);
            return res1;
        }
    }
}
