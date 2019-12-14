using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using BSU.Util;
using BSU.CoreInterface;
using BSU.Hashes;

namespace BSU.Core
{
    // TODO: document that this is (aggressively) using normalized (=lower case) paths!
    public class DirectoryStorage : IStorage
    {
        private readonly string _path, _name;

        public DirectoryStorage(string path, string name)
        {
            _path = path;
            _name = name;
        }

        public virtual List<ILocalMod> GetMods()
        {
            return GetModFolders().Select(di => (ILocalMod)new DirectoryMod(di, this)).ToList();
        }

        protected List<DirectoryInfo> GetModFolders()
        {
            return new DirectoryInfo(_path).EnumerateDirectories("@*").ToList();
        }

        public string GetLocation() => _path;

        public string GetIdentifier() => _name;
        public ILocalMod CreateMod(string identifier)
        {
            var dir = new DirectoryInfo(Path.Combine(_path, "@" + identifier));
            if (dir.Exists) throw new InvalidOperationException("Path exists");
            dir.Create();
            return new DirectoryMod(dir, this);
        }

        public virtual bool CanWrite() => true;
    }

    public class DirectoryMod : ILocalMod
    {
        private readonly DirectoryInfo _dir;
        private readonly IStorage _parentStorage;

        public DirectoryMod(DirectoryInfo dir, IStorage parentStorage)
        {
            _dir = dir;
            _parentStorage = parentStorage;
        }

        public bool FileExists(string path) => File.Exists(GetFullFilePath(path));

        public string GetDisplayName()
        {
            var modCpp = GetFile("/mod.cpp");
            string modCppData = null;
            if (modCpp != null)
            {
                using var reader = new StreamReader(modCpp);
                modCppData = reader.ReadToEnd();
            }

            var keys = GetFileList().Where(p => Regex.IsMatch(p, "/keys/.*\\.bikey", RegexOptions.IgnoreCase))
                .Select(p => p.Split('/').Last().Replace(".bikey", "")).ToList();

            return Util.Util.GetDisplayName(modCppData, keys);
        }

        public Stream GetFile(string path)
        {
            return File.OpenRead(GetFullFilePath(path));
        }

        public List<string> GetFileList()
        {
            var files = _dir.EnumerateFiles("*", SearchOption.AllDirectories);
            return files.Select(fi => fi.FullName.Replace(_dir.FullName, "").Replace('\\', '/').ToLowerInvariant()).ToList();
        }

        public FileHash GetFileHash(string path)
        {
            CheckPath(path);
            var extension = Utils.GetExtension(path).ToLowerInvariant();
            return new SHA1AndPboHash(GetFile(path), extension);
        }

        public string GetIdentifier() => _dir.Name;

        public IStorage GetStorage() => _parentStorage;

        public void DeleteFile(string path)
        {
            if (!_parentStorage.CanWrite()) throw new NotSupportedException();
            File.Delete(GetFullFilePath(path));
        }

        public string GetFilePath(string path)
        {
            if (!_parentStorage.CanWrite()) throw new NotSupportedException();
            return GetFullFilePath(path);
        }

        private string GetFullFilePath(string path)
        {
            CheckPath(path);
            return Path.Combine(_dir.FullName, path.Substring(1));
        }

        private static void CheckPath(string path)
        {
            if (!path.StartsWith('/')) throw new FormatException();
            if (path.Contains('\\')) throw new FormatException();
            if (path.ToLowerInvariant() != path) throw new FormatException();
        }
    }
}
