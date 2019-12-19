using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BSU.BSO.FileStructures;
using IniParser;

namespace BSU.Server
{
    class ServerConfig
    {
        public string SourcePath, TargetPath, ModList, ServerFileName;
        public ServerFile Server;

        public static ServerConfig Load(string path)
        {
            var data = new FileIniDataParser().ReadFile(path);
            var server = new ServerFile
            {
                ServerName = data["Server"]["Name"],
                ServerAddress = data["Server"]["Address"],
                Password = data["Server"]["Password"],
                ServerPort = int.Parse(data["Server"]["Port"]),
                LastUpdateDate = DateTime.Parse(data["Server"]["LastUpdate"]),
                ServerGuid = Guid.Parse(data["Server"]["Guid"]),
                SyncUris = data["Server"]["SyncUris"].Split(",").Select(u => new Uri(u)).ToList(),
                CreationDate = DateTime.Parse(data["Server"]["CreationDate"]),
                ModFolders = new List<ModFolder>()
            };
            return new ServerConfig
            {
                ServerFileName = data["Misc"]["ServerFileName"],
                SourcePath = data["Preset"]["SourcePath"],
                TargetPath = data["Preset"]["TargetPath"],
                ModList = data["Preset"]["ModList"],
                Server = server
            };
        }
    }
}
