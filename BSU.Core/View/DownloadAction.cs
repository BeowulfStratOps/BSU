using System;

namespace BSU.Core.View
{
    public class DownloadAction
    {
        internal Model.Storage Storage;

        public string StorageName => Storage.Identifier;

        internal DownloadAction(Model.Storage storage)
        {
            Storage = storage;
        }

        public void DoDownload()
        {
            throw new NotImplementedException();
        }
    }
}