using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BSU.Core.Hashes;
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

        private State GetState()
        {
            // TODO: make this less ugly
            // TODO: make hash-handling less ugly

            var state = new State();

            foreach (var storage in _state.GetStorages())
            {
                foreach (var localMod in storage.GetMods())
                {
                    Console.WriteLine($"Hashing {storage.GetName()}/{localMod.GetIdentifier()}");
                    state.Hashes.GetMatchHash(localMod);
                    state.Hashes.GetVersionHash(localMod);
                }
            }

            foreach (var repository in _state.GetRepositories())
            {
                Console.WriteLine("Repo " + repository.GetName());
                foreach (var remoteMod in repository.GetMods())
                {
                    Console.WriteLine("Checking " + remoteMod.GetIdentifier());
                    var matchHash = state.Hashes.GetMatchHash(remoteMod);
                    var matching = _state.GetStorages().SelectMany(s => s.GetMods())
                        .Where(m => matchHash.IsMatch(state.Hashes.GetMatchHash(m)));
                    var modActions = new ModActions();
                    var versionHash = state.Hashes.GetVersionHash(remoteMod);
                    foreach (var localMod in matching)
                    {
                        var test = state.Hashes.GetVersionHash(localMod);
                        if (versionHash.Matches(state.Hashes.GetVersionHash(localMod)))
                            modActions.Use.Add(localMod);
                        else
                            modActions.Update.Add(localMod);
                    }

                    state.Actions[remoteMod] = modActions;
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