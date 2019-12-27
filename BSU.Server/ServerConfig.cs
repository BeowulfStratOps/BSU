using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BSU.BSO.FileStructures;
using IniParser;
using IniParser.Model;

namespace BSU.Server
{
    internal class ServerConfig
    {
        public string SourcePath, TargetPath, ModList, ServerFileName;
        public ServerFile Server;

        public static ServerConfig Load(string path)
        {
            if (!File.Exists(path))
            {
                Console.WriteLine("File not found: " + path);
                throw new FileNotFoundException(path);
            }

            var data = new FileIniDataParser().ReadFile(path);
            var server = new ServerFile
            {
                ServerName = data.GetData("Server", "Name"),
                ServerAddress = data.GetData("Server", "Address"),
                Password = data.GetData("Server", "Password"),
                ServerPort = int.Parse(data.GetData("Server", "Port")),
                LastUpdateDate = DateTime.Parse(data.GetData("Server", "LastUpdate")),
                ServerGuid = Guid.Parse(data.GetData("Server", "Guid")),
                SyncUris = data.GetData("Server", "SyncUris").Split(",").Select(u => new Uri(u)).ToList(),
                CreationDate = DateTime.Parse(data.GetData("Server", "CreationDate")),
                ModFolders = new List<ModFolder>()
            };
            return new ServerConfig
            {
                ServerFileName = data.GetData("Misc", "ServerFileName"),
                SourcePath = data.GetData("Preset", "SourcePath"),
                TargetPath = data.GetData("Preset", "TargetPath"),
                ModList = data.GetData("Preset", "ModList"),
                Server = server
            };
        }
    }

    internal static class IniHelper
    {
        public static string GetData(this IniData data, string section, string key)
        {
            if (data.TryGetKey(section + data.SectionKeySeparator + key, out var value)) return value;
            Console.WriteLine($"Missing ini entry {section}.{key}");
            throw new KeyNotFoundException(section + "." + data);
        }
    }
}
