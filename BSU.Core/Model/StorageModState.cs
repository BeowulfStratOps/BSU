using System;
using System.Threading.Tasks;
using BSU.Core.Hashes;
using BSU.Core.Persistence;

namespace BSU.Core.Model
{
    internal interface IStorageModState
    {
        Task<StorageModStateEnum> GetState();
        Task<VersionHash> GetVersionHash();
        Task<MatchHash> GetMatchHash();
    }
}