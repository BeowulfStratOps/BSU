using System;
using System.IO;
using System.Linq;
using BSU.Core.Ioc;
using BSU.Core.Model;
using BSU.Core.Storage;
using BSU.Core.ViewModel.Util;

namespace BSU.Core.ViewModel
{
    public class AddStorage : ObservableBase
    {
        private readonly IModel _model;
        private bool _isDirectory = true;
        private bool _isSteam;

        internal AddStorage(IServiceProvider services, bool allowSteam)
        {
            _model = services.Get<IModel>();
            Ok = new DelegateCommand(HandleOk);
            if (!allowSteam || _model.GetStorages().Any(s => s.Name.ToLowerInvariant() == "steam")) return;
            SteamPath = SteamStorage.GetWorkshopPath();
            SteamEnabled = SteamPath != null ;
        }

        public bool SteamEnabled { get; }

        private void HandleOk(object? objWindow)
        {
            if (IsDirectory)
            {
                var valName = ValidateName();
                var valPath = ValidatePath();
                if (!valName || !valPath) return;
            }

            ((ICloseable)objWindow!).Close(true);
        }

        public string Path
        {
            get => _path;
            set
            {
                if (_path == value) return;
                _path = value;
                ValidatePath();
            }
        }

        public string Name
        {
            get => _name;
            set
            {
                if (_name == value) return;
                _name = value;
                ValidateName();
            }
        }

        public bool IsDirectory
        {
            get => _isDirectory;
            set
            {
                if (_isDirectory == value) return;
                _isDirectory = value;
                OnPropertyChanged();
            }
        }

        public bool IsSteam
        {
            get => _isSteam;
            set
            {
                if (_isSteam == value) return;
                _isSteam = value;
                OnPropertyChanged();
            }
        }

        private string _nameError = "";
        public string NameError
        {
            get => _nameError;
            private set
            {
                if (_nameError == value) return;
                _nameError = value;
                OnPropertyChanged();
            }
        }

        private string _pathError = "";
        private string _path = "";
        private string _name = "";

        public string PathError
        {
            get => _pathError;
            private set
            {
                if (_pathError == value) return;
                _pathError = value;
                OnPropertyChanged();
            }
        }

        public string? SteamPath { get; }

        public DelegateCommand Ok { get; }

        private bool ValidateName()
        {
            if (string.IsNullOrWhiteSpace(Name))
            {
                NameError = "Missing Name";
                return false;
            }

            if (Name.Trim().ToLowerInvariant() == "steam")
            {
                NameError = "Reserved Name";
                return false;
            }

            if (_model.GetStorages().Any(s => string.Equals(s.Name, Name, StringComparison.InvariantCultureIgnoreCase)))
            {
                NameError = "Name in use";
                return false;
            }

            NameError = "";
            return true;
        }

        private bool ValidatePath()
        {
            if (string.IsNullOrWhiteSpace(Path))
            {
                PathError = "Missing Path";
                return false;
            }

            // TODO: make sure it's isn't contained by another path, or contains another path.

            PathError = "";
            return true;
        }

        public string GetStorageType() => IsDirectory ? "DIRECTORY" : "STEAM";
        public string GetName() => IsDirectory ? Name : "Steam";
        public string GetPath() => IsDirectory ? Path : SteamPath!;
    }
}
