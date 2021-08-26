using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BSU.CoreCommon
{
    /// <summary>
    /// Repository of mods. Likely on a remote server.
    /// </summary>
    public interface IRepository
    {
        Task<Dictionary<string, IRepositoryMod>> GetMods(CancellationToken cancellationToken);
    }
}
