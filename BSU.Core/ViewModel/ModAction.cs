using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using BSU.Core.Model;
using BSU.Core.Services;
using BSU.Core.ViewModel.Util;

namespace BSU.Core.ViewModel
{
    public abstract class ModAction : ObservableBase, IEquatable<ModAction>
    {
        internal static ModAction Create(ModSelection selection, IModelRepositoryMod parent)
        {
            return selection switch
            {
                ModSelectionNone => new SelectNone(),
                ModSelectionLoading => new SelectLoading(),
                ModSelectionDisabled => new SelectDisabled(),
                ModSelectionDownload download => new SelectStorage(download.DownloadStorage),
                ModSelectionStorageMod actionStorageMod => new SelectMod(actionStorageMod.StorageMod,
                    CoreCalculation.GetModAction(parent, actionStorageMod.StorageMod)),
                _ => throw new ArgumentException()
            };
        }


        public abstract bool Equals(ModAction? other);

        public override bool Equals(object? obj)
        {
            return ReferenceEquals(this, obj) || obj is ModAction other && Equals(other);
        }

        public override int GetHashCode() => 0;

        internal abstract ModSelection AsSelection { get; }
    }

    // TODO: give the simple ones a static Instance to avoid creating new ones all the time?

    public class SelectDisabled : ModAction
    {
        public override bool Equals(ModAction? other) => other is SelectDisabled;

        internal override ModSelection AsSelection => new ModSelectionDisabled();
    }

    public class SelectNone : ModAction
    {
        public override bool Equals(ModAction? other) => other is SelectNone;

        internal override ModSelection AsSelection => new ModSelectionDisabled();
    }

    public class SelectLoading : ModAction
    {
        public override bool Equals(ModAction? other) => other is SelectLoading;

        internal override ModSelection AsSelection => throw new InvalidOperationException();
    }

    public class SelectMod : ModAction
    {
        internal IModelStorageMod StorageMod { get; }
        public ModActionEnum ActionType { get; }

        public string Name { get; }

        public string StorageName => StorageMod.ParentStorage.Name;

        internal SelectMod(IModelStorageMod storageMod, ModActionEnum actionType)
        {
            StorageMod = storageMod;
            ActionType = actionType;
            Name = storageMod.GetTitle();
        }

        public override bool Equals(ModAction? other)
        {
            return other is SelectMod selectMod && selectMod.StorageMod == StorageMod && selectMod.ActionType == ActionType;
        }

        internal override ModSelection AsSelection => new ModSelectionStorageMod(StorageMod);
    }

    public class SelectStorage : ModAction
    {
        internal IModelStorage DownloadStorage { get; }
        public string Name => DownloadStorage.Name;

        internal SelectStorage(IModelStorage downloadStorage)
        {
            DownloadStorage = downloadStorage;
        }

        public override bool Equals(ModAction? other)
        {
            return other is SelectStorage selectMod && selectMod.DownloadStorage == DownloadStorage;
        }

        internal override ModSelection AsSelection => new ModSelectionDownload(DownloadStorage);
    }
}
