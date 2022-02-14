using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BSU.CoreCommon;
using BSU.Hashes;

namespace BSU.Core.Tests.Mocks
{
    internal class MockStorageMod : IStorageMod, IMockedFiles
    {
        private readonly Task _load;
        public bool ThrowErrorOpen = false;

        private readonly Dictionary<string, byte[]> _files = new();

        public MockStorageMod(Task? load = null)
        {
            _load = load ?? Task.CompletedTask;
        }

        public IReadOnlyDictionary<string, string> GetFiles()
        {
            return _files.ToDictionary(kv => kv.Key, kv => Encoding.UTF8.GetString(kv.Value));
        }

        public void SetFile(string key, string data)
        {
            _files[key] = Encoding.UTF8.GetBytes(data);
        }

        public async Task<List<string>> GetFileList(CancellationToken cancellationToken)
        {
            await _load;
            return _files.Keys.ToList();
        }

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

        public async Task<FileHash> GetFileHash(string path, CancellationToken cancellationToken)
        {
            await _load;
            await using var stream = await OpenRead(path, cancellationToken);
            var hash = await SHA1AndPboHash.BuildAsync(stream!, Utils.GetExtension(path), CancellationToken.None);
            return hash;
        }

        public async Task<string> GetTitle(CancellationToken cancellationToken) => "Test";
        public string Path => throw new NotImplementedException();

        public async Task<Stream> OpenWrite(string path, CancellationToken cancellationToken)
        {
            await _load;
            if (ThrowErrorOpen) throw new TestException();
            var data = _files.TryGetValue(path, out var content) ? content : Array.Empty<byte>();
            return new MockStream(data, d => _files[path] = d);
        }

        public async Task<Stream?> OpenRead(string path, CancellationToken cancellationToken)
        {
            await _load;
            if (ThrowErrorOpen) throw new TestException();
            var result = _files.TryGetValue(path, out var content) ? new MemoryStream(content) : null;
            return result;
        }

        public Task Move(string @from, string to, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<bool> HasFile(string path, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task Delete(string path, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }

    internal class TestException : Exception
    {
    }
}
