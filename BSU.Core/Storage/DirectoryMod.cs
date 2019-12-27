using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using BSU.CoreCommon;
using BSU.Hashes;
using NLog;

namespace BSU.Core.Storage
{
    public class DirectoryMod : IStorageMod
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private readonly DirectoryInfo _dir;
        private readonly IStorage _parentStorage;
        private string _displayName;

        private readonly Uid _uid = new Uid();

        public Uid GetUid() => _uid;

        public DirectoryMod(DirectoryInfo dir, IStorage parentStorage)
        {
            _dir = dir;
            _parentStorage = parentStorage;
        }

        public string GetDisplayName()
        {
            if (_displayName != null) return _displayName;

            var modCpp = GetFile("/mod.cpp");
            string modCppData = null;
            if (modCpp != null)
            {
                using var reader = new StreamReader(modCpp);
                modCppData = reader.ReadToEnd();
            }

            var keys = GetFileList().Where(p => Regex.IsMatch(p, "/keys/.*\\.bikey", RegexOptions.IgnoreCase))
                .Select(p => p.Split('/').Last().Replace(".bikey", "")).ToList();

            return _displayName = Util.GetDisplayName(modCppData, keys);
        }

        public Stream GetFile(string path)
        {
            try
            {
                Logger.Trace("{0} Reading file {1}", _uid, path);
                return File.OpenRead(GetFullFilePath(path));
            }
            catch (FileNotFoundException)
            {
                return null;
            }
        }

        public List<string> GetFileList()
        {
            var files = _dir.EnumerateFiles("*", SearchOption.AllDirectories);
            return files.Select(fi => fi.FullName.Replace(_dir.FullName, "").Replace('\\', '/').ToLowerInvariant())
                .ToList();
        }

        public FileHash GetFileHash(string path)
        {
            Util.CheckPath(path);
            var extension = Utils.GetExtension(path).ToLowerInvariant();
            var file = GetFile(path);
            return file == null ? null : new SHA1AndPboHash(file, extension);
        }

        public string GetIdentifier() => _dir.Name;

        public IStorage GetStorage() => _parentStorage;

        public void DeleteFile(string path)
        {
            Logger.Trace("Deleting file {0}", path);
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
            Util.CheckPath(path);
            return Path.Combine(_dir.FullName, path.Substring(1));
        }
    }
}
