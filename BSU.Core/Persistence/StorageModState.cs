﻿using System;
using System.Collections.Generic;
using NLog;

namespace BSU.Core.Persistence
{
    internal interface IPersistedStorageModState
    {
        UpdateTarget? UpdateTarget { get; set; }
    }

    internal class PersistedStorageModState : IPersistedStorageModState
    {
        private readonly Dictionary<string, UpdateTarget> _updating;
        private readonly Action _store;
        private readonly string _identifier;
        private readonly ILogger _logger;

        public PersistedStorageModState(Dictionary<string, UpdateTarget> updating, Action store, string identifier)
        {
            _logger = LogHelper.GetLoggerWithIdentifier(this, identifier);
            _updating = updating;
            _store = store;
            _identifier = identifier;
        }

        public UpdateTarget? UpdateTarget
        {
            get => _updating.GetValueOrDefault(_identifier);
            set
            {
                _logger.Debug($"Set updating: {_identifier} to {value}");
                if (value == null)
                    _updating.Remove(_identifier);
                else
                    _updating[_identifier] = value;
                _store();
            }
        }
    }
}
