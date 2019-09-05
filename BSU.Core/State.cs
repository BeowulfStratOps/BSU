using System;
using System.Collections.Generic;
using System.Text;

namespace BSU.Core
{
    public class GlobalState
    {
        public List<RepoState> Repositories;
        public List<StorageState> Storages;
    }

    public class RepoState
    {
        public string Name, Location;
        public bool IsUpToDate;
        public List<RepoModState> Mods;
    }

    public class RepoModState
    {
        public RepoState Parent;
        public string Name;
        public bool IsUpToDate;
        public List<ModAction> Actions;
    }

    public enum ModActionType
    {
        Download,
        Use,
        AwaitUpdate,
        Update
    }

    public class ModAction
    {
        public List<RepoState> Conflicts;
        public ModActionType Type;
        public StorageState Storage;
        public StorageModState Mod;
        public RepoModState Parent;

        public bool IsSelected;
        public void Select()
        {
            throw new NotImplementedException();
        }
    }

    public class StorageState
    {
        public List<StorageModState> Mods;
    }

    public class StorageModState
    {
        public StorageState Parent;
        public List<RepoModState> UsedBy;

        // can't be broken and updating.
        public bool IsBroken;
        public RepoModState UpdatingTo;
    }
}
