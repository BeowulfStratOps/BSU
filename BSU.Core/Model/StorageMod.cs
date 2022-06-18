using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using BSU.Core.Concurrency;
using BSU.Core.Hashes;
using BSU.Core.Ioc;
using BSU.Core.Model.Updating;
using BSU.Core.Persistence;
using BSU.Core.Sync;
using BSU.CoreCommon;
using BSU.CoreCommon.Hashes;
using NLog;

namespace BSU.Core.Model
{
    internal class StorageMod : IModelStorageMod
    {
        private readonly IPersistedStorageModState _internalState;
        public bool CanWrite { get; }
        public string Identifier { get; }
        public IModelStorage ParentStorage { get; }
        public bool IsDeleted { get; private set; }
        private readonly IStorageMod _implementation;

        private readonly HashManager _hashes = new();
        private string? _title;

        private readonly ILogger _logger;

        private StorageModStateEnum _state = StorageModStateEnum.Loading; // TODO: should not be directly accessible
        private readonly IDispatcher _dispatcher;

        private StorageModStateEnum State
        {
            get => _state;
            set
            {
                var old = _state;
                _state = value;
                _logger.Debug($"Changing state from {old} to {value}");
                StateChanged?.Invoke();
            }
        }

        public StorageMod(IStorageMod implementation, string identifier,
            IPersistedStorageModState internalState, IModelStorage parent, bool canWrite, IServiceProvider services)
        {
            _logger = LogHelper.GetLoggerWithIdentifier(this, identifier);
            _internalState = internalState;
            ParentStorage = parent;
            CanWrite = canWrite;
            _dispatcher = services.Get<IDispatcher>();
            _jobManager = services.Get<IJobManager>();
            _updateService = services.Get<IUpdateService>();
            _implementation = implementation;
            Identifier = identifier;

            _hashes.JobCompleted += () => _dispatcher.ExecuteSynchronized(() => StateChanged?.Invoke());

            var updateTarget = _internalState.UpdateTarget;
            if (updateTarget != null)
            {
                _hashes.Set(updateTarget.Hashes);
                _title = updateTarget.Title;
                _state = StorageModStateEnum.CreatedWithUpdateTarget;
            }
            else
            {
                Load(false);
            }
        }

        private async Task<string> LoadAsync(CancellationToken cancellationToken)
        {
            var title = await _implementation.GetTitle(cancellationToken);

            return title;
        }

        private static readonly Regex BikeyRegex = new("^/keys/([^/]+.bikey)$", RegexOptions.Compiled);
        private readonly IJobManager _jobManager;
        private readonly IUpdateService _updateService;

        public async Task<Dictionary<string, byte[]>> GetKeyFiles(CancellationToken cancellationToken)
        {
            var result = new Dictionary<string, byte[]>();

            foreach (var file in await _implementation.GetFileList(cancellationToken))
            {
                var match = BikeyRegex.Match(file);
                if (!match.Success) continue;
                var name = match.Groups[1].Value;

                using var stream = await _implementation.OpenRead(file, cancellationToken);
                var content = new byte[stream!.Length];
                stream.Read(content);
                // TODO: make sure we get everything
                // TODO: make async
                result.Add(name, content);
            }

            return result;
        }

        private void Load(bool withUpdateTarget)
        {
            _jobManager.Run(() => LoadAsync(CancellationToken.None), getResult =>
            {
                try
                {
                    _title = getResult();
                    
                    foreach (var (type, func) in _implementation.GetHashFunctions())
                    {
                        _hashes.AddHashFunction(type, func);
                    }
                    SetState(
                        withUpdateTarget ? StorageModStateEnum.CreatedWithUpdateTarget : StorageModStateEnum.Created,
                        StorageModStateEnum.Loading);
                }
                catch (Exception e)
                {
                    _logger.Error(e);
                    // TODO: should this be reported to user directly?
                    SetState(StorageModStateEnum.Error, StorageModStateEnum.Loading);
                }
            }, CancellationToken.None);
        }

        public Task<IModHash> GetHash(Type type)
        {
            if (State == StorageModStateEnum.Loading) throw new InvalidOperationException($"Not allowed in State {State}");
            return _hashes.GetHash(type);
        }

        public List<Type> GetSupportedHashTypes() => _hashes.GetSupportedTypes();

        public StorageModStateEnum GetState() => State;

        public string GetTitle() => _title ?? Identifier;

        public void Delete(bool removeData)
        {
            if (removeData) throw new NotImplementedException();
            IsDeleted = true;
        }

        public string GetAbsolutePath() => _implementation.Path;

        public event Action? StateChanged;

        public async Task<UpdateResult> Update(IRepositoryMod repositoryMod, UpdateTarget target,
            IProgress<FileSyncStats>? progress, CancellationToken cancellationToken)
        {
            _internalState.UpdateTarget = target;
            var resetTask = _hashes.Reset(target.Hashes);
            SetState(StorageModStateEnum.Updating, StorageModStateEnum.Created,
                StorageModStateEnum.CreatedWithUpdateTarget);
            await resetTask;

            var result = await _updateService.UpdateAsync(repositoryMod, _implementation, cancellationToken, progress);

            _dispatcher.ExecuteSynchronized(() =>
            {
                if (result == UpdateResult.Success)
                {
                    _internalState.UpdateTarget = null;
                    SetState(StorageModStateEnum.Created, StorageModStateEnum.Updating);
                }
                else
                {
                    SetState(StorageModStateEnum.CreatedWithUpdateTarget, StorageModStateEnum.Updating);
                }
            });
            return result;
        }

        // ReSharper disable once ParameterOnlyUsedForPreconditionCheck.Local
        private void SetState(StorageModStateEnum newState, params StorageModStateEnum[] acceptableCurrent)
        {
            if (!acceptableCurrent.Contains(State)) throw new InvalidOperationException($"Tried to transition from {State} to {newState}");
            State = newState;
        }

        public void Abort()
        {
            SetState(StorageModStateEnum.Created, StorageModStateEnum.CreatedWithUpdateTarget);
        }

        public PersistedSelection GetStorageModIdentifiers()
        {
            return new PersistedSelection(PersistedSelectionType.StorageMod, ParentStorage.Identifier, Identifier);
        }

        public override string ToString() => Identifier;
    }
}
