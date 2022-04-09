using System;
using System.IO;

namespace BSU.Core.Model
{
    // TODO: better name
    internal abstract class ModSelection : IEquatable<ModSelection>
    {
        public abstract bool Equals(ModSelection? other);

        public override int GetHashCode() => 0;

        public override bool Equals(object? obj)
        {
            return Equals(obj as ModSelection);
        }
    }

    internal sealed class ModSelectionDisabled : ModSelection
    {
        public override string ToString() => "Disabled";
        public override bool Equals(ModSelection? other) => other is ModSelectionDisabled;
    }

    internal sealed class ModSelectionNone : ModSelection
    {
        public override string ToString() => "None";
        public override bool Equals(ModSelection? other) => other is ModSelectionNone;
    }

    internal sealed class ModSelectionLoading : ModSelection
    {
        public override string ToString() => "Loading";
        public override bool Equals(ModSelection? other) => other is ModSelectionLoading;
    }

    internal sealed class ModSelectionStorageMod : ModSelection
    {
        public IModelStorageMod StorageMod { get; }
        public ModSelectionStorageMod(IModelStorageMod storageMod)
        {
            StorageMod = storageMod;
        }

        public override string ToString() => $"Mod:{StorageMod}";
        public override bool Equals(ModSelection? other)
        {
            return other is ModSelectionStorageMod otherSm && otherSm.StorageMod.Equals(StorageMod);
        }
    }

    internal sealed class ModSelectionDownload : ModSelection
    {
        public IModelStorage DownloadStorage { get; }
        public string DownloadName { get; }

        public ModSelectionDownload(IModelStorage storage, string name)
        {
            DownloadStorage = storage;
            DownloadName = name;
        }

        public bool IsNameValid(out string error)
        {
            if (!DownloadName.StartsWith("@"))
            {
                error = "Must start with a '@'";
                return false;
            }

            if (DownloadName.IndexOfAny(Path.GetInvalidPathChars()) >= 0 ||
                DownloadName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            {
                error = "Contains illegal characters";
                return false;
            }

            if (string.IsNullOrWhiteSpace(DownloadName))
            {
                error = "Is empty";
                return false;
            }

            error = "";
            return true;
        }

        public override string ToString() => $"Storage:{DownloadStorage.Name}/{DownloadName}";
        public override bool Equals(ModSelection? other)
        {
            return other is ModSelectionDownload download && download.DownloadStorage.Equals(DownloadStorage) && download.DownloadName == DownloadName;
        }
    }
}
