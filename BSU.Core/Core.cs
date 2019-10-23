using System;
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
        private readonly ISettings _settings;
        private readonly InternalState _state;

        public Core(FileInfo settingsPath)
        {
            _settings = Settings.Load(settingsPath);
            _state = new InternalState(_settings);
        }

        public Core(ISettings settings)
        {
            _settings = settings;
            _state = new InternalState(settings);
        }

        public void AddRepoType(string name, Func<string, string, IRepository> create) => _state.AddRepoType(name, create);
        public void AddStorageType(string name, Func<string, string, IStorage> create) => _state.AddStorageType(name, create);

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