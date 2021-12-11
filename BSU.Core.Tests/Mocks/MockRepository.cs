using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BSU.CoreCommon;
using Xunit;

namespace BSU.Core.Tests.Mocks
{
    internal class MockRepository : IRepository
    {
        public Dictionary<string, IRepositoryMod> Mods { get; } = new();
        private readonly Task _load;

        public MockRepository(Task? load)
        {
            _load = load ?? Task.CompletedTask;
        }

        public async Task<Dictionary<string, IRepositoryMod>> GetMods(CancellationToken cancellationToken)
        {
            await _load;
            return Mods;
        }

        public async Task<ServerInfo> GetServerInfo(CancellationToken cancellationToken)
        {
            await _load;
            return new ServerInfo("test", "test");
        }
    }
}
