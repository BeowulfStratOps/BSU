using System.Collections.Generic;

namespace BSU.CoreCommon
{
    public interface IStorage
    {
        /// <summary>
        /// e.g. false steam
        /// </summary>
        /// <returns></returns>
        bool CanWrite();

        Dictionary<string, IStorageMod> GetMods();

        IStorageMod CreateMod(string identifier);
        void RemoveMod(string identifier);
        public void Load();
    }
}
