using System.Collections.Generic;

namespace BSU.CoreCommon
{
    public interface IRepository
    {
        List<IRepositoryMod> GetMods();

        /// <summary>
        /// alias. identifier of sorts.
        /// </summary>
        /// <returns></returns>
        string GetName();

        /// <summary>
        /// Url or smth
        /// </summary>
        /// <returns></returns>
        string GetLocation();
        Uid GetUid();
    }
}
