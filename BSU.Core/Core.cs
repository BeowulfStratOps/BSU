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

        private Dictionary<IRemoteMod, ModActions> GetState()
        {
            // TODO: make this less ugly

            var state = new Dictionary<IRemoteMod, ModActions>();

            var localHashes = new Dictionary<ILocalMod, Tuple<MatchHash, VersionHash>>();

            foreach (var storage in _state.GetStorages())
            {
                foreach (var localMod in storage.GetMods())
                {
                    Console.WriteLine($"Hashing {storage.GetName()}/{localMod.GetIdentifier()}");
                    localHashes.Add(localMod,
                        Tuple.Create(MatchHash.FromLocalMod(localMod), VersionHash.FromLocalMod(localMod)));
                }
            }

            foreach (var repository in _state.GetRepositories())
            {
                Console.WriteLine("Repo " + repository.GetName());
                foreach (var remoteMod in repository.GetMods())
                {
                    Console.WriteLine("Checking " + remoteMod.GetIdentifier());
                    var matchHash = MatchHash.FromRemoteMod(remoteMod);
                    var matching = localHashes.Where(kv => kv.Value.Item1.IsMatch(matchHash)).Select(kv => kv.Key);
                    var modActions = new ModActions();
                    var versionHash = VersionHash.FromRemoteMod(remoteMod);
                    foreach (var localMod in matching)
                    {
                        if (versionHash.Matches(localHashes[localMod].Item2))
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