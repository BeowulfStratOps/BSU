using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BSU.CoreCommon;

namespace BSU.Core.Tests.Mocks
{
    internal class MockRepository : IRepository
    {
        public Dictionary<string, IRepositoryMod> Mods { get; } = new();
        private Action<MockRepository> _load;

        public MockRepository(Action<MockRepository> load = null)
        {
            _load = load;
        }

        public Task<Dictionary<string, IRepositoryMod>> GetMods(CancellationToken cancellationToken)
        {
            _load?.Invoke(this);
            _load = null;
            return Task.FromResult(Mods);
        }

        public Task<ServerInfo> GetServerInfo(CancellationToken cancellationToken)
        {
            return Task.FromResult<ServerInfo>(null);
        }
    }
}
