using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

        private Dictionary<IRemoteMod, ModActions> GetState()
        {
            var state = new Dictionary<IRemoteMod, ModActions>();

            foreach (var repository in _state.GetRepositories())
            {
                Console.WriteLine("Repo " + repository.GetName());
                foreach (var remoteMod in repository.GetMods())
                {
                    Console.WriteLine("Checking " + remoteMod.GetIdentifier());
                    var matching =
                        remoteMod.GetMatchingMods(_state.GetStorages().SelectMany(s => s.GetMods()).ToList());
                    var modActions = new ModActions();
                    foreach (var localMod in matching)
                    {
                        if (remoteMod.IsVersionMatching(localMod))
                            modActions.Use.Add(localMod);
                        else
                            modActions.Update.Add(localMod);
                    }

                    state[remoteMod] = modActions;
                }
            }

            return state;
        }

        /// <summary>
        /// Does all the hard work. Don't spam it.
        /// </summary>
        /// <returns></returns>
        public ViewState GetViewState()
        {
            return new ViewState(_state.GetRepositories(), _state.GetStorages(), GetState());
        }

        public void PrintInternalState() => _state.PrintState();
    }
}