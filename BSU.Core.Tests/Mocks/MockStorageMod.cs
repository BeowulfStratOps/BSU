using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BSU.CoreCommon;
using BSU.Hashes;
using NLog;

namespace BSU.Core.Tests.Mocks
{
    internal class MockStorageMod : IStorageMod, IMockedFiles
    {
        public string Identifier;
        public MockStorage Storage;
        public bool Locked = false;
        public bool ThrowErrorLoad = false;
        public bool ThrowErrorOpen = false;
        private readonly Action<MockStorageMod> _load;
        private readonly Logger _logger = EntityLogger.GetLogger();

        public Dictionary<string, byte[]> Files = new Dictionary<string, byte[]>();

        public MockStorageMod(Action<MockStorageMod> load = null)
        {
            _load = load;
        }

        public IReadOnlyDictionary<string, string> GetFiles()
        {
            return Files.ToDictionary(kv => kv.Key, kv => Encoding.UTF8.GetString(kv.Value));
        }

        public void SetFile(string key, string data)
        {
            if (Locked) throw new IOException("File in use");
            Files[key] = Encoding.UTF8.GetBytes(data);
        }

        public string GetFileContent(string key)
        {
            if (Locked) throw new IOException("File in use");
            return Encoding.UTF8.GetString(Files[key]);
        }

        public void DeleteFile(string path)
        {
            if (Locked) throw new IOException("File in use");
            Files.Remove(path);
        }

        public string GetDisplayName() => throw new NotImplementedException();

        public Task<Stream> OpenFile(string path, FileAccess access, CancellationToken cancellationToken)
        {
            if (Locked) throw new IOException("File in use");

            if (ThrowErrorOpen) throw new TestException();

            if (access.HasFlag(FileAccess.Write))
            {
                var data = Files.TryGetValue(path, out var content) ? content : Array.Empty<byte>();
                return Task.FromResult<Stream>(new MockStream(data, d => Files[path] = d));
            }
            else
            {
                var result = Files.TryGetValue(path, out var content) ? new MemoryStream(content) : null;
                return Task.FromResult<Stream>(result);
            }
        }

        /*public FileHash GetFileHash(string path)
        {
            if (Locked) throw new IOException("File in use");
            return new SHA1AndPboHash(OpenFile(path, FileAccess.Read), Utils.GetExtension(path));
        }*/

        public Task<List<string>> GetFileList(CancellationToken cancellationToken) => Task.FromResult(Files.Keys.ToList());

        public string GetIdentifier() => Identifier;

        public IStorage GetStorage() => Storage;

        public string GetFilePath(string path)
        {
            if (Locked) throw new IOException("File in use");
            return "";
        }

        public void Load()
        {
            if (ThrowErrorLoad) throw new TestException();
            _load?.Invoke(this);
        }

        public int GetUid() => _logger.GetId();

        private sealed class MockStream : MemoryStream
        {
            private readonly Action<byte[]> _save;

            public MockStream(byte[] data, Action<byte[]> save) : base()
            {
                _save = save;
                Write(data, 0, data.Length);
                Seek(0, SeekOrigin.Begin);
            }

            protected override void Dispose(bool disposing)
            {
                _save(ToArray());
                base.Dispose(disposing);
            }
        }

        public Task<string> GetDisplayName(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<FileHash> GetFileHash(string path, CancellationToken cancellationToken)
        {
            using var stream = OpenFile(path, FileAccess.Read, cancellationToken).Result;
            var hash = SHA1AndPboHash.BuildAsync(stream, Utils.GetExtension(path), CancellationToken.None).Result;
            return Task.FromResult<FileHash>(hash);
        }

        public Task DeleteFile(string path, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }

    internal class TestException : Exception
    {
    }
}
