using System.Collections.Generic;

namespace BSU.CoreCommon
{
    /// <summary>
    /// Repository of mods. Likely on a remote server.
    /// </summary>
    public interface IRepository
    {
        void Load();

        /// <summary>
        /// List of mods the repository contains..
        /// </summary>
        /// <returns></returns>
        Dictionary<string, IRepositoryMod> GetMods();
    }
}
