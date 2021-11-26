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
        public bool ThrowErrorOpen = false;

        private readonly Dictionary<string, byte[]> _files = new();

        public IReadOnlyDictionary<string, string> GetFiles()
        {
            return _files.ToDictionary(kv => kv.Key, kv => Encoding.UTF8.GetString(kv.Value));
        }

        public void SetFile(string key, string data)
        {
            _files[key] = Encoding.UTF8.GetBytes(data);
        }

        public Task<Stream> OpenFile(string path, FileAccess access, CancellationToken cancellationToken)
        {
            if (ThrowErrorOpen) throw new TestException();

            if (access.HasFlag(FileAccess.Write))
            {
                var data = _files.TryGetValue(path, out var content) ? content : Array.Empty<byte>();
                return Task.FromResult<Stream>(new MockStream(data, d => _files[path] = d));
            }
            else
            {
                var result = _files.TryGetValue(path, out var content) ? new MemoryStream(content) : null;
                return Task.FromResult<Stream>(result);
            }
        }

        public Task<List<string>> GetFileList(CancellationToken cancellationToken) => Task.FromResult(_files.Keys.ToList());

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
            using var stream = await OpenFile(path, FileAccess.Read, cancellationToken);
            var hash = await SHA1AndPboHash.BuildAsync(stream, Utils.GetExtension(path), CancellationToken.None);
            return hash;
        }

        public Task DeleteFile(string path, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public async Task<string> GetTitle(CancellationToken cancellationToken) => "Test";
    }

    internal class TestException : Exception
    {
    }
}
