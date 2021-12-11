using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BSU.CoreCommon;

namespace BSU.Core.Tests.Mocks
{
    internal class MockStorage : IStorage
    {
        private readonly Task _load;
        public readonly Dictionary<string, MockStorageMod> Mods = new();

        public MockStorage(Task? load)
        {
            _load = load ?? Task.CompletedTask;
        }

        public bool CanWrite() => true;
        public async Task<Dictionary<string, IStorageMod>> GetMods(CancellationToken cancellationToken)
        {
            await _load;
            var mods = Mods.ToDictionary(kv => kv.Key, kv => (IStorageMod) kv.Value);
            return mods;
        }

        public async Task<IStorageMod> CreateMod(string identifier, CancellationToken cancellationToken)
        {
            await _load;
            if (identifier == null) throw new ArgumentNullException();
            var newMod = new MockStorageMod(_load);
            Mods.Add(identifier, newMod);
            return newMod;
        }

        public async Task RemoveMod(string identifier, CancellationToken cancellationToken)
        {
            await _load;
            Mods.Remove(identifier);
        }
    }
}
