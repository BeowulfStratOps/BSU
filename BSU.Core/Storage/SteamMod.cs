using System.IO;
using BSU.CoreCommon;

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
    }
}
