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

        public void DoDownload(string name = null)
        {
            name ??= Parent.Name;
            new Thread(() =>
            {
                Storage.PrepareDownload(Parent.Mod.Implementation, Parent.Mod.AsUpdateTarget, name, e => {}, update =>
                {
                    update.OnPrepared += update.Commit;
                });
            }).Start();
        }
    }
}