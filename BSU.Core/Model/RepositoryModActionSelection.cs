using System;

namespace BSU.Core.Model
{
    // TODO: better name
    internal abstract class RepositoryModActionSelection : IEquatable<RepositoryModActionSelection>
    {
        public abstract bool Equals(RepositoryModActionSelection other);

        public abstract override int GetHashCode();

        public override bool Equals(object obj)
        {
            return Equals(obj as RepositoryModActionSelection);
        }
    }

    internal sealed class RepositoryModActionDoNothing : RepositoryModActionSelection
    {
        public override string ToString() => "Do Nothing";
        public override bool Equals(RepositoryModActionSelection other) => other is RepositoryModActionDoNothing;

        public override int GetHashCode() => typeof(RepositoryModActionDoNothing).GetHashCode();
    }

    internal sealed class RepositoryModActionStorageMod : RepositoryModActionSelection
    {
        public IModelStorageMod StorageMod { get; }
        public RepositoryModActionStorageMod(IModelStorageMod storageMod)
        {
            StorageMod = storageMod;
        }

        public override string ToString() => $"Mod:{StorageMod}";
        public override bool Equals(RepositoryModActionSelection other)
        {
            return other is RepositoryModActionStorageMod otherSm && otherSm.StorageMod.Equals(StorageMod);
        }

        public override int GetHashCode() => HashCode.Combine(StorageMod);
    }

    internal sealed class RepositoryModActionDownload : RepositoryModActionSelection
    {
        public IModelStorage DownloadStorage { get; }
        public RepositoryModActionDownload(IModelStorage storage)
        {
            DownloadStorage = storage;
        }

        public override string ToString() => $"Storage:{DownloadStorage}";
        public override bool Equals(RepositoryModActionSelection other)
        {
            return other is RepositoryModActionDownload download && download.DownloadStorage.Equals(DownloadStorage);
        }

        public override int GetHashCode() => HashCode.Combine(DownloadStorage);
    }
}
