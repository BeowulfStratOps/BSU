using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using BSU.CoreCommon;
using BSU.Hashes;

namespace BSU.Core.Tests
{
    internal class TmpBackedStorageMod : IStorageMod
    {
        public readonly string Identifier, DisplayName;
        public TmpBackedStorage Storage;
        private readonly DirectoryInfo _baseTmp;

        public TmpBackedStorageMod(DirectoryInfo baseTmp, string identifier)
        {
            _baseTmp = baseTmp;
            Identifier = identifier;
            Directory.CreateDirectory(Path.Combine(_baseTmp.FullName, identifier));
        }

        private FileInfo GetFileInfo(string path)
        {
            return new FileInfo(Path.Combine(_baseTmp.FullName, Identifier, path));
        }

        public void SetFile(string key, string data)
        {
            File.WriteAllBytes(GetFileInfo(key).FullName, Encoding.UTF8.GetBytes(data));
        }

        public string GetFileContent(string key) => Encoding.UTF8.GetString(File.ReadAllBytes(GetFileInfo(key).FullName));

        public void DeleteFile(string path) => GetFileInfo(path).Delete();

        public bool FileExists(string path) => GetFileInfo(path).Exists;

        public string GetDisplayName() => DisplayName;

        public Stream GetFile(string path)
        {
            return GetFileInfo(path).Exists ? File.OpenRead(GetFileInfo(path).FullName) : null;
        }

        public FileHash GetFileHash(string path)
        {
            return new SHA1AndPboHash(GetFile(path), Utils.GetExtension(path));
        }

        public List<string> GetFileList()
        {
            var dir = new DirectoryInfo(Path.Combine(_baseTmp.FullName, Identifier));
            return dir.EnumerateFiles().Select(fi => fi.Name).ToList();
        }

        public string GetIdentifier() => Identifier;

        public IStorage GetStorage() => Storage;

        public string GetFilePath(string path) => GetFileInfo(path).FullName;
    }
}
