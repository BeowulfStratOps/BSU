﻿using System;
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
        internal static ModAction Create(RepositoryModActionSelection selection, IModelRepositoryMod parent)
        {
            if (selection == null) return null;

            if (selection is RepositoryModActionDoNothing) return new SelectDoNothing();

            if (selection is RepositoryModActionDownload download) return new SelectStorage(download.DownloadStorage);

            if (selection is RepositoryModActionStorageMod actionStorageMod)
                return new SelectMod(actionStorageMod.StorageMod, CoreCalculation.GetModAction(parent, actionStorageMod.StorageMod));

            throw new ArgumentException();
        }


        public abstract bool Equals(ModAction other);

        public override bool Equals(object obj)
        {
            return ReferenceEquals(this, obj) || obj is ModAction other && Equals(other);
        }

        public abstract override int GetHashCode();

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

        internal override RepositoryModActionSelection AsSelection => new RepositoryModActionDoNothing();
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

        public override bool Equals(ModAction other)
        {
            var ret = other is SelectMod selectMod && selectMod.StorageMod == StorageMod && selectMod.ActionType == ActionType;
            return ret;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(StorageMod);
        }

        internal override RepositoryModActionSelection AsSelection => new RepositoryModActionStorageMod(StorageMod);
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
            var ret = other is SelectStorage selectMod && selectMod.DownloadStorage == DownloadStorage;
            return ret;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(DownloadStorage);
        }

        internal override RepositoryModActionSelection AsSelection => new RepositoryModActionDownload(DownloadStorage);
    }
}
