using System;
using System.Collections.Generic;
using NLog;

namespace BSU.Core.Persistence
{
    internal class StorageModState : IStorageModState
    {
        private readonly Dictionary<string, UpdateTarget> _updating;
        private readonly Action _store;
        private readonly string _identifier;
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public StorageModState(Dictionary<string, UpdateTarget> updating, Action store, string identifier)
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
                Logger.Debug("Set updating: {0} to {1}", _identifier, value);
                if (value == null)
                    _updating.Remove(_identifier);
                else
                    _updating[_identifier] = value;
                _store();
            }
        }
    }
}