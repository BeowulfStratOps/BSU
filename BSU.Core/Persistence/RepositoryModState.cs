using System;
using System.Collections.Generic;

namespace BSU.Core.Persistence
{
    internal interface IPersistedRepositoryModState
    {
        PersistedSelection? Selection { get; set; }
    }

    internal class PersistedRepositoryModState : IPersistedRepositoryModState
    {
        private readonly Func<PersistedSelection?> _getUsedMod;
        private readonly Action<PersistedSelection?> _setUsedMod;

        public PersistedRepositoryModState(Func<PersistedSelection?> getUsedMod, Action<PersistedSelection?> setUsedMod)
        {
            _getUsedMod = getUsedMod;
            _setUsedMod = setUsedMod;
        }

        public PersistedSelection? Selection
        {
            get => _getUsedMod();
            set => _setUsedMod(value);
        }
    }
}
