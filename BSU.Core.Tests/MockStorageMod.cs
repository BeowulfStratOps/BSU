using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using BSU.CoreInterface;
using BSU.Hashes;

namespace BSU.Core.Tests
{
    internal class MockStorageMod : ILocalMod
    {
        public string Identifier, DisplayName;
        public MockStorage Storage;

        public Dictionary<string, byte[]> Files = new Dictionary<string, byte[]>();

        public void SetFile(string key, string data)
        {
            Files[key] = Encoding.UTF8.GetBytes(data);
        }

        public string GetFileContent(string key) => Encoding.UTF8.GetString(Files[key]);

        public void DeleteFile(string path) => Files.Remove(path);

        public bool FileExists(string path) => Files.ContainsKey(path);

        public string GetDisplayName() => DisplayName;

        public Stream GetFile(string path)
        {
            return Files.ContainsKey(path) ? new MemoryStream(Files[path]) : null;
        }

        public FileHash GetFileHash(string path)
        {
            return new SHA1AndPboHash(GetFile(path), Utils.GetExtension(path));
        }

        public List<string> GetFileList() => Files.Keys.ToList();

        public string GetIdentifier() => Identifier;

        public IStorage GetStorage() => Storage;

        public Stream OpenFile(string path, FileAccess access)
        {
            return new MockStream(Files, path);
        }
    }

    class MockStream : Stream
    {
        private readonly Dictionary<string, byte[]> _dict;
        private readonly string _path;

        public MockStream(Dictionary<string, byte[]> dict, string path)
        {
            _dict = dict;
            _path = path;
        }

        public void SetData(string key, byte[] data)
        {
            _dict[key] = data;
        }

        public override void Flush()
        {
            throw new System.NotImplementedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new System.NotImplementedException();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new System.NotImplementedException();
        }

        public override void SetLength(long value)
        {
            throw new System.NotImplementedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new System.NotImplementedException();
        }

        public override bool CanRead => throw new NotImplementedException();
        public override bool CanSeek => throw new NotImplementedException();
        public override bool CanWrite => throw new NotImplementedException();
        public override long Length => throw new NotImplementedException();
        public override long Position
        {
            get => throw new NotImplementedException();
            set => throw new NotImplementedException();
        }
    }
}