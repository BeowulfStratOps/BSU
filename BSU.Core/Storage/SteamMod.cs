using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using BSU.CoreCommon;
using Newtonsoft.Json.Linq;

namespace BSU.Core.Storage
{
    /// <summary>
    /// Mod within a steam-workshop folder. Read-only.
    /// </summary>
    public class SteamMod : DirectoryMod
    {
        public SteamMod(DirectoryInfo directory, IStorage parentStorage) : base(directory, parentStorage)
        {
            // Directory mod should consider the writable flag of it's parent storage
        }

        public override async Task<string> GetTitle(CancellationToken cancellationToken)
        {
            var dirName = Dir.Name;
            try
            {
                var fileId = ulong.Parse(dirName);
                using var client = new HttpClient();
                var content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    {"itemcount", "1"},
                    {"publishedfileids[0]", fileId.ToString()},
                });
                var result = await client.PostAsync("https://api.steampowered.com/ISteamRemoteStorage/GetPublishedFileDetails/v1",
                    content, cancellationToken);
                result.EnsureSuccessStatusCode();
                var response = await result.Content.ReadAsStringAsync(cancellationToken);
                var root = JToken.Parse(response);
                var results = (JArray)root["response"]["publishedfiledetails"];
                var title = (string)results[0]["title"];
                return title;
            }
            catch (Exception e)
            {
                Logger.Error(e);
                return dirName;
            }
        }
    }
}
