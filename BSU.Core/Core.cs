﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BSU.Core.Hashes;
using BSU.Core.State;
using BSU.CoreInterface;

namespace BSU.Core
{
    public class Core
    {
        private readonly Settings _settings;
        private readonly InternalState _state;

        public Core(FileInfo settingsPath)
        {
            _settings = Settings.Load(settingsPath);
            _state = new InternalState(_settings);
        }

        public void AddRepo(string name, string url, string type) => _state.AddRepo(name, url, type);

        public void AddStorage(string name, DirectoryInfo directory, string type) =>
            _state.AddStorage(name, directory, type);

        /// <summary>
        /// Does all the hard work. Don't spam it.
        /// </summary>
        /// <returns></returns>
        public State.State GetState()
        {
            return new State.State(_state.GetRepositories(), _state.GetStorages(), this);
        }


        public void PrintInternalState() => _state.PrintState();

        public UpdateTarget GetUpdateTarget(StorageMod mod) => _state.GetUpdateTarget(mod);
    }
}