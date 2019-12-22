using System.IO;
using System.Threading;
using BSU.CoreCommon;
using Microsoft.VisualBasic.CompilerServices;

namespace BSU.Core.Sync
{
    internal class UpdateAction : WorkUnit
    {
        private readonly IRepositoryMod _repository;
        private readonly long _sizeTotal;
        private long _sizeTodo;

        public UpdateAction(IRepositoryMod repository, IStorageMod storage, string path, long sizeTotal, RepoSync sync) : base(storage, path, sync)
        {
            _repository = repository;
            _sizeTotal = sizeTotal;
            _sizeTodo = sizeTotal;
        }

        public long GetBytesTotal() => _sizeTotal;
        public long GetBytesRemaining() => _sizeTodo;

        protected override void DoWork()
        {
            var target = Storage.GetFilePath(Path.ToLowerInvariant());
            _repository.UpdateTo(Path, target, UpdateRemaining);
            _sizeTodo = 0;
        }

        void UpdateRemaining(long bytesDownloaded)
        {
            _sizeTodo -= bytesDownloaded;
        }
    }
}
