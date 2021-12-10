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
        private Action<MockRepository>? _load;
        private readonly int _ioDelayMs;

        public MockRepository(Action<MockRepository>? load = null, int ioDelayMs = 0)
        {
            _load = load;
            _ioDelayMs = ioDelayMs;
        }

        public Task<Dictionary<string, IRepositoryMod>> GetMods(CancellationToken cancellationToken)
        {
            if (_load != null)
            {
                Thread.Sleep(_ioDelayMs);
                _load?.Invoke(this);
            }

            _load = null;
            return Task.FromResult(Mods);
        }

        public Task<ServerInfo> GetServerInfo(CancellationToken cancellationToken)
        {
            return Task.FromResult(new ServerInfo("test", "test"));
        }
    }
}
