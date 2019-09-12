using System;
using System.Collections.Generic;
using System.Text;
using BSU.CoreInterface;

namespace BSU.Core
{
    public class ViewState
    {
        public List<RepoView> Repositories;
        public List<StorageView> Storages;
    }

    public class RepoView
    {
        public string Name, Location;
        public bool IsUpToDate;
        public List<RepoModView> Mods;
    }

    public class RepoModView
    {
        public RepoView Parent;
        public string Name;
        public bool IsUpToDate;
        public List<ModActionView> Actions;
        public List<StorageModView> Candidates;
    }

    public enum ModActionType
    {
        Download,
        Use,
        AwaitUpdate,
        Update
    }

    public class ModActionView
    {
        public List<RepoView> Conflicts;
        public ModActionType Type;
        public StorageView Storage;
        public StorageModView Mod;
        public RepoModView Parent;

        public bool IsSelected;
        public void Select()
        {
            throw new NotImplementedException();
        }
    }

    public class StorageView
    {
        public List<StorageModView> Mods;
    }

    public class StorageModView
    {
        public string Name, DisplayName, Location;
        public StorageView Parent;
        public List<RepoModView> UsedBy;

        // can't be broken and updating.
        public bool IsBroken;
        public RepoModView UpdatingTo;
    }
}
