using System;
using System.Threading.Tasks;
using BSU.Core.Hashes;

namespace BSU.Core.Model
{
    internal interface IRepositoryModState
    {
        Task<MatchHash> GetMatchHash();
        Task<MatchHash> GetVersionHash();
    }
}