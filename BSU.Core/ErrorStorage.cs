using System;
using System.Collections.Generic;
using BSU.CoreCommon;

namespace BSU.Core
{
    internal class ErrorStorage : IStorage
    {
        private readonly string _storageName;
        private readonly string _storagePath;
        private readonly string _eMessage;

        public ErrorStorage(string storageName, string storagePath, string eMessage)
        {
            _storageName = storageName;
            _storagePath = storagePath;
            _eMessage = eMessage;
        }

        public bool CanWrite() => false;

        public List<IStorageMod> GetMods() => new List<IStorageMod>();

        public string GetLocation() => _storagePath;

        public string GetIdentifier() => _storageName;

        public IStorageMod CreateMod(string identifier) => throw new NotSupportedException();
        public Uid GetUid()
        {
            throw new NotImplementedException();
        }
    }
}
