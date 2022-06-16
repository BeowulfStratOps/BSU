using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BSU.Core.Hashes;
using BSU.Core.Tests.Mocks;
using BSU.CoreCommon;
using BSU.CoreCommon.Hashes;
using BSU.Hashes;
using Moq;
using Xunit;

namespace BSU.Core.Tests
{
    public class VersionHashTests
    {
        [Fact]
        private async Task Simple1()
        {
            Assert.True(await Check(new Dictionary<string, string>(), new Dictionary<string, string>()));
        }

        [Fact]
        private async Task Simple2()
        {
            Assert.True(await Check(new Dictionary<string, string>
                {
                    {"some.file", "stuff!"}
                },
                new Dictionary<string, string>
                {
                    {"some.file", "stuff!"}
                }));
        }

        [Fact]
        private async Task Simple3()
        {
            Assert.True(await Check(new Dictionary<string, string>
                {
                    {"some.pbo", "xxAAAABBBBCCCCDDDDEEEE"}
                },
                new Dictionary<string, string>
                {
                    {"some.pbo", "xxAAAABBBBCCCCDDDDEEEE"}
                }));
        }

        [Fact]
        private async Task PboTrust()
        {
            Assert.True(await Check(new Dictionary<string, string>
                {
                    {"some.pbo", "xxAAAABBBBCCCCDDDDEEEE"}
                },
                new Dictionary<string, string>
                {
                    {"some.pbo", "yyAAAABBBBCCCCDDDDEEEE"}
                }));
        }

        [Fact]
        private async Task False1()
        {
            Assert.False(await Check(new Dictionary<string, string>
                {
                    {"some.pbo", "xxAAAABBBBCCCCDDDDEEEE"}
                },
                new Dictionary<string, string>
                {
                    {"some.pbo", "xxAAAABBBB___CCCDDDDEEEE"}
                }));
        }

        [Fact]
        private async Task True1()
        {
            Assert.True(await Check(new Dictionary<string, string>
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
        private async Task False2()
        {
            Assert.False(await Check(new Dictionary<string, string>
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
        private async Task False3()
        {
            Assert.False(await Check(new Dictionary<string, string>
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

        private static async Task<VersionHash> CreateStorageMod(Dictionary<string, string> files)
        {
            var mod = new Mock<IStorageMod>(MockBehavior.Strict);
            mod.Setup(m => m.GetFileList(It.IsAny<CancellationToken>())).Returns(Task.FromResult(files.Keys.ToList()));
            mod.Setup(m => m.OpenRead(It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns((string path, CancellationToken _) =>
                Task.FromResult<Stream?>(new MemoryStream(Encoding.UTF8.GetBytes(files[path]))));
            return await VersionHash.CreateAsync(mod.Object, CancellationToken.None);
        }

        private static async Task<VersionHash> CreateRepositoryMod(Dictionary<string, string> files)
        {
            var mod = new Mock<IRepositoryMod>(MockBehavior.Strict);
            mod.Setup(m => m.GetFileList(It.IsAny<CancellationToken>())).Returns(Task.FromResult(files.Keys.ToList()));
            mod.Setup(m => m.GetFileHash(It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns((string path, CancellationToken _) =>
            {
                var data = Encoding.UTF8.GetBytes(files[path]);
                using var stream = new MemoryStream(data);
                var hash = Sha1AndPboHash.BuildAsync(stream, Utils.GetExtension(path), CancellationToken.None).Result;
                return Task.FromResult<FileHash>(hash);
            });
            return await VersionHash.CreateAsync(mod.Object, CancellationToken.None);
        }

        private static async Task<bool> Check(Dictionary<string, string> files1, Dictionary<string, string> files2)
        {
            var res1 = (await CreateStorageMod(files1)).IsMatch(await CreateRepositoryMod(files2));
            var res2 = (await CreateRepositoryMod(files1)).IsMatch(await CreateStorageMod(files2));
            Assert.Equal(res1, res2);
            return res1;
        }
    }
}
