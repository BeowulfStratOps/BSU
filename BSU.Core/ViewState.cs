using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BSU.Core.Hashes;
using BSU.CoreInterface;

namespace BSU.Core
{
    public class ViewState
    {
        public readonly List<RepoView> Repos;
        public readonly List<StorageView> Storages;

        internal ViewState(IReadOnlyList<IRepository> repos, IReadOnlyList<IStorage> storages, State state)
        {
            Storages = storages.Select(s => new StorageView(s)).ToList();
            Repos = repos.Select(r => new RepoView(r, state, Storages)).ToList();
        }
    }

    public class RepoView
    {
        public readonly List<RepoModView> Mods;
        public readonly string Name;

        internal RepoView(IRepository repo, State state, List<StorageView> storages)
        {
            Mods = new List<RepoModView>();
            foreach (var remoteMod in repo.GetMods())
            {
                var actions = state.Actions.GetValueOrDefault(remoteMod, new ModActions());
                var modView = new RepoModView(remoteMod, actions, storages, state.Hashes.GetVersionHash(remoteMod));
                Mods.Add(modView);
            }
            Name = repo.GetName();
        }
    }

    public class RepoModView
    {
        public readonly string Name;
        public readonly IReadOnlyList<ModActionView> Actions;
        public ModActionView Selected = null;

        internal RepoModView(IRemoteMod mod, ModActions modActions, List<StorageView> storages, VersionHash version)
        {
            Name = mod.GetIdentifier();
            var actions = new List<ModActionView>();

            // TODO: grab already existing storageModView??
            actions.AddRange(modActions.Use.Select(l => new UseActionView(new StorageModView(l))));
            actions.AddRange(modActions.Update.Select(l => new UpdateActionView(new StorageModView(l), version.GetHashString(), mod.GetDisplayName())));

            actions.AddRange(storages.Where(s => s.CanWrite).Select(s => new DownloadActionView(s)));

            Actions = actions.AsReadOnly();
            if (actions.Any(a => a is UseActionView))
                Selected = actions[0];
        }
    }

    public class ModActionView
    {
    }

    public class UseActionView : ModActionView
    {
        public readonly StorageModView LocalMod;

        internal UseActionView(StorageModView localMod)
        {
            LocalMod = localMod;
        }

        public override string ToString() => "Use " + LocalMod.Location;
    }

    public class UpdateActionView : ModActionView
    {
        public readonly StorageModView LocalMod;
        public readonly string VersionHash, VersionDisplay;

        internal UpdateActionView(StorageModView localMod, string versionHash, string versionDisplay)
        {
            LocalMod = localMod;
            VersionHash = versionHash;
            VersionDisplay = versionDisplay;
        }

        public override string ToString() => $"Update {LocalMod.Location} to {VersionHash} \"{VersionDisplay}\"";
    }

    public class DownloadActionView : ModActionView
    {
        public readonly StorageView Storage;

        internal DownloadActionView(StorageView storage)
        {
            Storage = storage;
        }

        public override string ToString() => "Download to " + Storage.Location;
    }

    public class StorageView
    {
        public readonly List<StorageModView> Mods;
        public readonly string Location;
        public readonly bool CanWrite;

        internal StorageView(IStorage storage)
        {
            Mods = storage.GetMods().Select(m => new StorageModView(m)).ToList();
            Location = storage.GetLocation();
            CanWrite = storage.CanWrite();
        }
    }

    public class StorageModView
    {
        public readonly string Name, Location;

        internal StorageModView(ILocalMod mod)
        {
            Name = mod.GetIdentifier();
            Location = mod.GetBaseDirectory().FullName;
        }

        /*public string Name, DisplayName, Location;
        public StorageView Parent;
        public List<RepoModView> UsedBy;

        // can't be broken and updating.
        public bool IsBroken;
        public RepoModView UpdatingTo;*/
    }
}
