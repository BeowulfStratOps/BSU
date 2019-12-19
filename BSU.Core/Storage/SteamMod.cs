using System.IO;
using BSU.CoreCommon;

namespace BSU.Core.Storage
{
    public class SteamMod : DirectoryMod
    {
        public SteamMod(DirectoryInfo directory, IStorage parentStorage) : base(directory, parentStorage)
        {
            // Directory mod should consider the writable flag of it's parent storage
        }
    }
}
