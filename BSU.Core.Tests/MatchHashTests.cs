using System;
using BSU.Core.Hashes;
using Xunit;

namespace BSU.Core.Tests
{
    public class MatchHashTests
    {
        [Fact]
        private void Simple1()
        {
            Assert.True(Check(Array.Empty<string>(), null, Array.Empty<string>(), null));
        }

        [Fact]
        private void Simple2()
        {
            Assert.True(Check(Array.Empty<string>(), "qw123", Array.Empty<string>(), "qw123"));
        }

        [Fact]
        private void Simple3()
        {
            Assert.True(Check(new[] {"/addons/qwe.pbo"}, null, new[] {"/addons/qwe.pbo"}, null));
        }

        [Fact]
        private void Simple4()
        {
            Assert.True(Check(new[] {"/addons/qwe.pbo"}, "qw123", new[] {"/addons/qwe.pbo"}, "qw123"));
        }

        [Fact]
        private void True1()
        {
            Assert.True(Check(new[] {"/addons/qwe.pbo", "/addons/asdf.pbo", "/addons/xyz.pbo"}, "gg",
                new[] {"/addons/qwe.pbo", "/addons/asdf.pbo", "/addons/abc.pbo"}, "gg"));
        }

        [Fact]
        private void True2()
        {
            Assert.True(Check(new[] {"/addons/qwe.pbo", "/addons/asdf.pbo", "/addons/xyz.pbo"}, "gg",
                new[] {"/addons/qwe_f.pbo", "/addons/asdf_f.pbo", "/addons/abc.pbo"}, "gg"));
        }

        [Fact]
        private void False3()
        {
            Assert.False(Check(new[] {"/addons/qwe.pbo", "/addons/asdf.pbo", "/addons/xyz.pbo"}, null,
                new[] {"/addons/qwe.pbo", "/addons/asdf.pbo", "/addons/abc.pbo"}, null));
        }

        [Fact]
        private void False0()
        {
            Assert.False(Check(new[] {"/addons/qwe.pbo", "/addons/asdf.pbo", "/addons/xyz.pbo"}, "gl",
                new[] {"/addons/qwe.pbo", "/addons/asdf.pbo", "/addons/xyz.pbo"}, "hf"));
        }

        [Fact]
        private void False1()
        {
            Assert.False(Check(new[] {"/addons/qwe1.pbo", "/addons/asdf1.pbo", "/addons/xyz.pbo"}, null,
                new[] {"/addons/qwe2.pbo", "/addons/asdf2.pbo", "/addons/abc.pbo"}, null));
        }

        [Fact]
        private void False2()
        {
            Assert.False(Check(new[] {"/addons/qwe.pbo.bisign", "/addons/asdf.pbo.bisign", "/addons/xyz.pbo"}, null,
                new[] {"/addons/qwe.pbo.bisign", "/addons/asdf.pbo.bisign", "/addons/abc.pbo"}, null));
        }

        // TODO: test keys

        private static void AddFiles(IMockedFiles mod, string[] names, string name)
        {
            foreach (var fileName in names)
            {
                mod.SetFile(fileName, "");
            }

            if (name != null) mod.SetFile("/mod.cpp", "name=\"" + name.Replace("\"", "\\\"") + "\";");
        }

        private static MatchHash CreateStorageMod(string[] names, string name)
        {
            var storageMod = new MockStorageMod();
            AddFiles(storageMod, names, name);
            return new MatchHash(storageMod);
        }

        private static MatchHash CreateRepoMod(string[] names, string name)
        {
            var repoMod = new MockRepositoryMod();
            AddFiles(repoMod, names, name);
            return new MatchHash(repoMod);
        }

        private static bool Check(string[] fileNames1, string modName1, string[] fileNames2, string modName2)
        {
            var res1 = CreateStorageMod(fileNames1, modName1).IsMatch(CreateRepoMod(fileNames2, modName2));
            var res2 = CreateRepoMod(fileNames1, modName1).IsMatch(CreateStorageMod(fileNames2, modName2));
            Assert.Equal(res1, res2);
            return res1;
        }
    }
}