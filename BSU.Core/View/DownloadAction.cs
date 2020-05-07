using System;
using System.Threading;

namespace BSU.Core.View
{
    public class DownloadAction
    {
        internal Model.Storage Storage;
        
        public RepositoryMod Parent { get; }

        public string StorageName => Storage.Identifier;

        internal DownloadAction(Model.Storage storage, RepositoryMod parent)
        {
            Storage = storage;
            Parent = parent;
        }

        public void DoDownload()
        {
            new Thread(() => Storage.StartDownload(Parent.Mod, Parent.Name, Storage.Model.MatchMaker)).Start();
        }
    }
}