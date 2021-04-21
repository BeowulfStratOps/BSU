using System;
using System.Collections.Generic;
using BSU.CoreCommon;
using NLog;

namespace BSU.Core.Persistence
{
    internal interface IPersistedStorageModState
    {
        UpdateTarget UpdateTarget { get; set; }
    }

    internal class PersistedStorageModState : IPersistedStorageModState
    {
        private readonly Dictionary<string, UpdateTarget> _updating;
        private readonly Action _store;
        private readonly string _identifier;
        private readonly Logger _logger = EntityLogger.GetLogger();

        public PersistedStorageModState(Dictionary<string, UpdateTarget> updating, Action store, string identifier)
        {
            _updating = updating;
            _store = store;
            _identifier = identifier;
        }

        public UpdateTarget UpdateTarget
        {
            get => _updating.GetValueOrDefault(_identifier);
            set
            {
                _logger.Debug("Set updating: {0} to {1}", _identifier, value);
                if (value == null)
                    _updating.Remove(_identifier);
                else
                    _updating[_identifier] = value;
                _store();
            }
        }
    }
}
