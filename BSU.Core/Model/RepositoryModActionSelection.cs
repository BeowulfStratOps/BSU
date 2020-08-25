using System;

namespace BSU.Core.Model
{
    // TODO: better name
    internal class RepositoryModActionSelection : IEquatable<RepositoryModActionSelection>
    {
        // TODO: provide type/kind of this selection for switching

        public bool DoNothing { get; }
        public IModelStorageMod StorageMod { get; }
        public IModelStorage DownloadStorage { get; }
        public string DownloadIdentifier { get; }

        public RepositoryModActionSelection(IModelStorageMod storageMod)
        {
            StorageMod = storageMod;
        }

        public RepositoryModActionSelection(IModelStorage storage, string downloadIdentifier)
        {
            DownloadStorage = storage;
            DownloadIdentifier = downloadIdentifier;
        }

        public RepositoryModActionSelection()
        {
            DoNothing = true;
        }

        public override string ToString()
        {
            if (DoNothing) return "DoNothing";
            if (StorageMod != null) return $"Mod:{StorageMod}";
            if (DownloadStorage != null) return $"Storage:{DownloadStorage} as {DownloadIdentifier}";
            throw new InvalidOperationException();
        }

        public bool Equals(RepositoryModActionSelection other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return DoNothing == other.DoNothing && Equals(StorageMod, other.StorageMod) && Equals(DownloadStorage, other.DownloadStorage) && DownloadIdentifier == other.DownloadIdentifier;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((RepositoryModActionSelection) obj);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(DoNothing, StorageMod, DownloadStorage, DownloadIdentifier);
        }

        public static bool operator ==(RepositoryModActionSelection left, RepositoryModActionSelection right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(RepositoryModActionSelection left, RepositoryModActionSelection right)
        {
            return !Equals(left, right);
        }
    }
}
