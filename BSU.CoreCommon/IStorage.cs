using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BSU.CoreCommon
{
    public interface IStorage
    {
        /// <summary>
        /// e.g. false for steam
        /// </summary>
        /// <returns></returns>
        bool CanWrite();

        Task<Dictionary<string, IStorageMod>> GetMods(CancellationToken cancellationToken);

        Task<IStorageMod> CreateMod(string identifier, CancellationToken cancellationToken);
        Task RemoveMod(string identifier, CancellationToken cancellationToken);
    }
}
