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

        public void AddStorage(string name, DirectoryInfo directory, string type) => _state.AddStorage(name, directory, type);

        /// <summary>
        /// Does all the hard work. Don't spam it.
        /// </summary>
        /// <returns></returns>
        public ViewState GetViewState()
        {
            var view = new ViewState {Repositories = new List<RepoView>()};

            foreach (var repository in _state.GetRepositories())
            {
                var repoView = new RepoView {Mods = new List<RepoModView>(), Name = repository.GetName()};
                view.Repositories.Add(repoView);
                Console.WriteLine("Repo " + repository.GetName());
                foreach (var remoteMod in repository.GetMods())
                {
                    Console.WriteLine("Checking " + remoteMod.GetIdentifier());
                    var modView = new RepoModView {Candidates = new List<Tuple<StorageModView, bool>>(), Name = remoteMod.GetIdentifier()};
                    repoView.Mods.Add(modView);
                    var matching = remoteMod.GetMatchingMods(_state.GetStorages().SelectMany(s => s.GetMods()).ToList());
                    foreach (var match in matching)
                    {
                        var storageModView = new StorageModView
                        {
                            Name = match.GetIdentifier()
                        };
                        modView.Candidates.Add(Tuple.Create(storageModView, remoteMod.IsVersionMatching(match)));
                    }
                }
            }

            return view;
        }

        public void PrintInternalState() => _state.PrintState();
    }
}