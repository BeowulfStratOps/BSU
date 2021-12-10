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
        private Action<MockStorage>? _load;
        private readonly int _ioDelayMs;
        public readonly Dictionary<string, MockStorageMod> Mods = new();

        public MockStorage(Action<MockStorage>? load = null, int ioDelayMs = 0)
        {
            _load = load;
            _ioDelayMs = ioDelayMs;
        }

        public bool CanWrite() => true;
        public Task<Dictionary<string, IStorageMod>> GetMods(CancellationToken cancellationToken)
        {
            Load();
            var mods = Mods.ToDictionary(kv => kv.Key, kv => (IStorageMod) kv.Value);
            return Task.FromResult(mods);
        }

        public Task<IStorageMod> CreateMod(string identifier, CancellationToken cancellationToken)
        {
            Load();
            Thread.Sleep(_ioDelayMs);
            if (identifier == null) throw new ArgumentNullException();
            var newMod = new MockStorageMod();
            Mods.Add(identifier, newMod);
            return Task.FromResult<IStorageMod>(newMod);
        }

        public Task RemoveMod(string identifier, CancellationToken cancellationToken)
        {
            Load();
            Mods.Remove(identifier);
            return Task.CompletedTask;
        }

        private void Load()
        {
            if (_load == null) return;
            _load?.Invoke(this);
            Thread.Sleep(_ioDelayMs);
            _load = null;
        }
    }
}
