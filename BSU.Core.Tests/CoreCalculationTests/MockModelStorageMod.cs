using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using BSU.Core.Hashes;
using BSU.Core.Model;
using BSU.Core.Model.Updating;
using BSU.Core.Persistence;
using BSU.Core.Sync;
using BSU.Core.Tests.Util;
using BSU.CoreCommon;
using BSU.CoreCommon.Hashes;

namespace BSU.Core.Tests.CoreCalculationTests
{
    internal class MockModelStorageMod : IModelStorageMod
    {
        private readonly HashCollection _hashes;
        private readonly StorageModStateEnum _state;

        public MockModelStorageMod(int? match, int? version, StorageModStateEnum state)
        {
            var hashes = new List<IModHash>();
            if (match != null)
                hashes.Add(TestUtils.GetMatchHash((int)match).Result);
            if (version != null)
                hashes.Add(TestUtils.GetVersionHash((int)version).Result);
            _hashes = new HashCollection(hashes.ToArray());
            _state = state;
        }


        public event Action? StateChanged;

        public Task<UpdateResult> Update(IRepositoryMod repositoryMod, UpdateTarget target, IProgress<FileSyncStats>? progress,
            CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public void Abort()
        {
            throw new NotImplementedException();
        }

        public PersistedSelection GetStorageModIdentifiers()
        {
            throw new NotImplementedException();
        }

        public bool CanWrite { get; set; } = true;
        public string Identifier { get; } = null!;
        public IModelStorage ParentStorage { get; } = null!;
        public bool IsDeleted { get; } = false;


        public StorageModStateEnum GetState() => _state;
        public string GetTitle()
        {
            throw new NotImplementedException();
        }

        public void Delete(bool removeData)
        {
            throw new NotImplementedException();
        }

        public string GetAbsolutePath()
        {
            throw new NotImplementedException();
        }
        public Task<Dictionary<string, byte[]>> GetKeyFiles(CancellationToken token)
        {
            throw new NotImplementedException();
        }
        
        public Task<IModHash> GetHash(Type type) => _hashes.GetHash(type);
        public List<Type> GetSupportedHashTypes() => _hashes.GetSupportedHashTypes();
    }
}
