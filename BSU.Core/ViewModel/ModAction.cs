using System;
using System.Collections.Generic;
using BSU.Core.Model;

namespace BSU.Core.ViewModel
{
    public class ModAction : ViewModelClass, IEquatable<ModAction>
    {
        internal readonly RepositoryModActionSelection Selection;

        private string _display;
        public string Display
        {
            get => _display;
            private set
            {
                if (value == _display) return;
                _display = value;
                OnPropertyChanged();
            }
        }

        internal ModAction(RepositoryModActionSelection selection, Dictionary<IModelStorageMod, Model.ModAction> actions)
        {
            Selection = selection;
            if (selection == null)
            {
                Display = "--";
                return;
            }

            if (selection.StorageMod != null)
            {
                var action = actions[selection.StorageMod];
                Display = $"{action.ActionType}";
                action.Updated += () => { Display = $"{action.ActionType}"; };
            }
            if (selection.DownloadStorage != null) Display = $"Download to {selection.DownloadStorage}";
            if (selection.DoNothing) Display = "Do nothing";
        }

        public bool Equals(ModAction other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Equals(Selection, other.Selection);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((ModAction) obj);
        }

        public override int GetHashCode()
        {
            return (Selection != null ? Selection.GetHashCode() : 0);
        }

        public static bool operator ==(ModAction left, ModAction right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(ModAction left, ModAction right)
        {
            return !Equals(left, right);
        }
    }
}