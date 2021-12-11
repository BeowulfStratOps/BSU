using System;
using BSU.Core.Model;

namespace BSU.Core.Persistence
{
    internal class PersistedSelection : IEquatable<PersistedSelection>
    {
        // TODO: might need a way to store download identifier?

        public PersistedSelectionType Type { get; }
        public Guid? Storage { get; }
        public string? Mod { get; }

        public PersistedSelection(PersistedSelectionType type, Guid? storage, string? mod)
        {
            Type = type;
            Storage = storage;
            Mod = mod;
        }

        public static PersistedSelection? FromSelection(ModSelection selection)
        {
            // TODO: PersistedSelection shouldn't have to know about None/Loading details
            return selection switch
            {
                ModSelectionNone => null,
                ModSelectionLoading => null,
                ModSelectionDisabled => new PersistedSelection(PersistedSelectionType.DoNothing, null, null),
                ModSelectionStorageMod storageModAction => storageModAction.StorageMod.GetStorageModIdentifiers(),
                ModSelectionDownload downloadAction => downloadAction.DownloadStorage.AsStorageIdentifier(),
                _ => throw new ArgumentException()
            };
        }

        public override string ToString() => $"{Storage?.ToString() ?? "-"}/{Mod??"-"}";

        public bool Equals(PersistedSelection? other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Type == other.Type && Nullable.Equals(Storage, other.Storage) && Mod == other.Mod;
        }

        public override bool Equals(object? obj)
        {
            return Equals(obj as PersistedSelection);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine((int)Type, Storage, Mod);
        }
    }

    internal enum PersistedSelectionType
    {
        DoNothing,
        StorageMod,
        Download
    }
}
