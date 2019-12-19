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

        List<ILocalMod> GetMods();

        /// <summary>
        /// path or smth
        /// </summary>
        /// <returns></returns>
        string GetLocation();

        /// <summary>
        /// alias. identifier of sorts
        /// </summary>
        /// <returns></returns>
        string GetIdentifier();

        ILocalMod CreateMod(string identifier);
    }
}
