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
        public List<ModState> Mods;
    }

    public class ModState
    {
        public List<ModAction> Actions;
    }

    public class ModAction
    {

    }

    public class StorageState
    {

    }
}
