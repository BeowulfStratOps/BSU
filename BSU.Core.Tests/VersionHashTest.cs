using System.Collections.Generic;
using BSU.Core.Hashes;
using BSU.Core.Tests.Mocks;
using Xunit;

namespace BSU.Core.Tests
{
    public class VersionHashTests
    {
        [Fact]
        private void Simple1()
        {
            Assert.True(Check(new Dictionary<string, string>(), new Dictionary<string, string>()));
        }

        [Fact]
        private void Simple2()
        {
            Assert.True(Check(new Dictionary<string, string>
                {
                    {"some.file", "stuff!"}
                },
                new Dictionary<string, string>
                {
                    {"some.file", "stuff!"}
                }));
        }

        [Fact]
        private void Simple3()
        {
            Assert.True(Check(new Dictionary<string, string>
                {
                    {"some.pbo", "xxAAAABBBBCCCCDDDDEEEE"}
                },
                new Dictionary<string, string>
                {
                    {"some.pbo", "xxAAAABBBBCCCCDDDDEEEE"}
                }));
        }

        [Fact]
        private void PboTrust()
        {
            Assert.True(Check(new Dictionary<string, string>
                {
                    {"some.pbo", "xxAAAABBBBCCCCDDDDEEEE"}
                },
                new Dictionary<string, string>
                {
                    {"some.pbo", "yyAAAABBBBCCCCDDDDEEEE"}
                }));
        }

        [Fact]
        private void False1()
        {
            Assert.False(Check(new Dictionary<string, string>
                {
                    {"some.pbo", "xxAAAABBBBCCCCDDDDEEEE"}
                },
                new Dictionary<string, string>
                {
                    {"some.pbo", "xxAAAABBBB___CCCDDDDEEEE"}
                }));
        }

        [Fact]
        private void True1()
        {
            Assert.True(Check(new Dictionary<string, string>
                {
                    {"some.pbo", "xxAAAABBBBCCCCDDDDEEEE"},
                    {"qwer.dll", "www"}
                },
                new Dictionary<string, string>
                {
                    {"some.pbo", "xxAAAABBBBCCCCDDDDEEEE"},
                    {"qwer.dll", "www"}
                }));
        }

        [Fact]
        private void False2()
        {
            Assert.False(Check(new Dictionary<string, string>
                {
                    {"some.pbo", "xxAAAABBBBCCCCDDDDEEEE"},
                    {"qwer.dll", "www"}
                },
                new Dictionary<string, string>
                {
                    {"some.pbo", "xxAAAABBBBCCCCDDDDEEEE"},
                    {"qwer.dll", "xyz"}
                }));
        }

        [Fact]
        private void False3()
        {
            Assert.False(Check(new Dictionary<string, string>
                {
                    {"some.pbo", "xxAAAABBBBCCCCDDDDEEEE"},
                    {"qwer.dll", "xyz"}
                },
                new Dictionary<string, string>
                {
                    {"some.pbo", "xxAAAABBBBCCCCDDDDEEEE"},
                    {"asdf.dll", "xyz"}
                }));
        }

        private static void AddFiles(IMockedFiles mod, Dictionary<string, string> files)
        {
            foreach (var file in files)
            {
                mod.SetFile(file.Key, file.Value);
            }
        }

        private static VersionHash CreateStorageMod(Dictionary<string, string> files)
        {
            var storageMod = new MockStorageMod();
            AddFiles(storageMod, files);
            return new VersionHash(storageMod);
        }

        private static VersionHash CreateRepositoryMod(Dictionary<string, string> files)
        {
            var repoMod = new MockRepositoryMod();
            AddFiles(repoMod, files);
            return new VersionHash(repoMod);
        }

        private static bool Check(Dictionary<string, string> files1, Dictionary<string, string> files2)
        {
            var res1 = CreateStorageMod(files1).IsMatch(CreateRepositoryMod(files2));
            var res2 = CreateRepositoryMod(files1).IsMatch(CreateStorageMod(files2));
            Assert.Equal(res1, res2);
            return res1;
        }
    }
}