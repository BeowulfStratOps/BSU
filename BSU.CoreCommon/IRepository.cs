using System.Collections.Generic;

namespace BSU.CoreCommon
{
    /// <summary>
    /// Repository of mods. Likely on a remote server.
    /// </summary>
    public interface IRepository
    {
        /// <summary>
        /// List of mods the repository contains..
        /// </summary>
        /// <returns></returns>
        List<IRepositoryMod> GetMods();

        /// <summary>
        /// Unique identifier within the application.
        /// </summary>
        /// <returns></returns>
        string GetIdentifier();

        /// <summary>
        /// Url or smth
        /// </summary>
        /// <returns></returns>
        string GetLocation();

        Uid GetUid();
    }
}
