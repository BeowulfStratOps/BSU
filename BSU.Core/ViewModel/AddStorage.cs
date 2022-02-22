using System;
using System.IO;
using System.Linq;
using BSU.Core.Ioc;
using BSU.Core.Model;
using BSU.Core.ViewModel.Util;

namespace BSU.Core.ViewModel
{
    public class AddStorage : ObservableBase
    {
        private readonly IModel _model;

        internal AddStorage(IServiceProvider services)
        {
            _model = services.Get<IModel>();
            Ok = new DelegateCommand(HandleOk);
        }

        private void HandleOk(object? objWindow)
        {
            var valName = ValidateName();
            var valPath = ValidatePath();
            if (!valName || !valPath) return;

            ((ICloseable)objWindow!).Close(true);
        }

        public string Path
        {
            get => _path;
            set
            {
                if (SetProperty(ref _path, value) && ValidatePath())
                    Name = new DirectoryInfo(value).Name;
            }
        }

        public string Name
        {
            get => _name;
            set
            {
                if (SetProperty(ref _name, value))
                    ValidateName();
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

        public string GetStorageType() =>"DIRECTORY";
        public string GetName() => Name;
        public string GetPath() => Path;
    }
}
