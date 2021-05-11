using System;
using System.Collections.Generic;
using BSU.Core.Model;
using BSU.Core.ViewModel.Util;

namespace BSU.Core.ViewModel
{
    public abstract class ModAction : ObservableBase, IEquatable<ModAction>
    {
        internal static ModAction Create(RepositoryModActionSelection selection, IModelRepositoryMod parent)
        {
            if (selection == null) return null;

            if (selection.DoNothing) return new SelectDoNothing();

            if (selection.DownloadStorage != null) return new SelectStorage(selection.DownloadStorage);

            if (selection.StorageMod != null)
                return new SelectMod(selection.StorageMod,
                    (ModActionEnum) CoreCalculation.GetModAction(parent, selection.StorageMod));

            throw new ArgumentException();
        }


        public abstract bool Equals(ModAction other);

        public override bool Equals(object obj)
        {
            return ReferenceEquals(this, obj) || obj is ModAction other && Equals(other);
        }

        public abstract override int GetHashCode();

        public static bool operator ==(ModAction left, ModAction right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(ModAction left, ModAction right)
        {
            return !Equals(left, right);
        }

        internal abstract RepositoryModActionSelection AsSelection { get; }
    }

    public class SelectDoNothing : ModAction
    {
        internal SelectDoNothing(){}

        public override bool Equals(ModAction other)
        {
            return other is SelectDoNothing;
        }

        public override int GetHashCode()
        {
            return 0;
        }

        internal override RepositoryModActionSelection AsSelection => new RepositoryModActionSelection();
    }

    public class SelectMod : ModAction
    {
        internal IModelStorageMod StorageMod { get; }
        public string ActionType { get; }
        public string Name => StorageMod.Identifier; // TODO: name

        internal SelectMod(IModelStorageMod storageMod, ModActionEnum actionType)
        {
            StorageMod = storageMod;
            ActionType = actionType.ToString();
        }

        public override bool Equals(ModAction other)
        {
            return other is SelectMod selectMod && selectMod.StorageMod == StorageMod;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(StorageMod);
        }

        internal override RepositoryModActionSelection AsSelection => new RepositoryModActionSelection(StorageMod);
    }

    public class SelectStorage : ModAction
    {
        internal IModelStorage DownloadStorage { get; }
        public string Name => DownloadStorage.Name;

        internal SelectStorage(IModelStorage downloadStorage)
        {
            DownloadStorage = downloadStorage;
        }

        public override bool Equals(ModAction other)
        {
            return other is SelectStorage selectMod && selectMod.DownloadStorage == DownloadStorage;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(DownloadStorage);
        }

        internal override RepositoryModActionSelection AsSelection => new RepositoryModActionSelection(DownloadStorage);
    }
}
