using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using BSU.Core.Ioc;
using BSU.Core.Model;
using BSU.Core.ViewModel.Util;

namespace BSU.Core.ViewModel
{
    public class AddRepository : ObservableBase
    {
        private readonly IModel _model;
        private string? _checkResult;
        private string? _checkError;
        private string _url = "";
        private string _name = "";
        private string _nameValidation = "";

        public string NameValidation
        {
            get => _nameValidation;
            set
            {
                if (_nameValidation == value) return;
                _nameValidation = value;
                OnPropertyChanged();
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

        private bool ValidateName()
        {
            if (string.IsNullOrWhiteSpace(Name))
            {
                NameValidation = "Must have a name";
                return false;
            }

            // TODO: trying to create 2 presets after another with the same name works for some reason
            if (_model.GetRepositories()
                .Any(r => string.Equals(r.Name, Name, StringComparison.InvariantCultureIgnoreCase)))
            {
                NameValidation = "Name in use";
                return false;
            }

            NameValidation = "";
            return true;
        }

        public string Url
        {
            get => _url;
            set
            {
                if (_url == value) return;
                _url = value;
                HandleUrlChanged();
            }
        }

        public string? CheckResult
        {
            get => _checkResult;
            private set
            {
                if (_checkResult == value) return;
                _checkResult = value;
                OnPropertyChanged();
            }
        }

        public string? CheckError
        {
            get => _checkError;
            private set
            {
                if (_checkError == value) return;
                _checkError = value;
                OnPropertyChanged();
            }
        }

        public ICommand Ok { get; }

        public List<KnownUrl> KnownUrls { get; } = new()
        {
            new KnownUrl("Beowulf", "http://sync.bso.ovh/server.json"),
            new KnownUrl("Beowulf 'NAAAAAM", "http://sync.bso.ovh/server-vn.json"),
            new KnownUrl("Beowulf - WS Compat", "http://sync.bso.ovh/server-compat.json")
        };

        internal AddRepository(IModel model, IServiceProvider services)
        {
            Ok = new DelegateCommand(HandleOkClick);
            _model = model;
            _services = services;
        }

        private void HandleOkClick(object? objWindow)
        {
            // TODO: at the moment, it's possible to click ok immediately after typing in a nonsensical url.
            var validationSucceeded = true;
            if (string.IsNullOrWhiteSpace(Url))
                CheckError = "Missing URL";
            if (!string.IsNullOrWhiteSpace(CheckError))
                validationSucceeded = false;
            if (!ValidateName()) validationSucceeded = false;

            if (validationSucceeded)
                ((ICloseable)objWindow!).Close(true);
        }

        private CancellationTokenSource _handlerDelayCts = new();
        private readonly IServiceProvider _services;

        private async void HandleUrlChanged()
        {
            CheckError = null;
            CheckResult = null;
            if (string.IsNullOrWhiteSpace(Url))
            {
                CheckError = "Missing URL";
                return;
            }
            _handlerDelayCts.Cancel();
            _handlerDelayCts = new CancellationTokenSource();
            try
            {
                await Task.Delay(500, _handlerDelayCts.Token);
            }
            catch
            {
                return;
            }
            _services.Get<IAsyncVoidExecutor>().Execute(() => CheckUrl(_handlerDelayCts.Token));
        }

        private async Task CheckUrl(CancellationToken cancellationToken)
        {
            CheckResult = "Checking ...";
            var result = await _model.CheckRepositoryUrl(Url, cancellationToken);
            if (result == null)
            {
                CheckResult = "";
                CheckError = "Invalid Sync URL";
                return;
            }
            CheckResult = $"Found Preset: {result.Name}";
            CheckError = "";
            if (string.IsNullOrWhiteSpace(Name)) // TODO: also replace it if the current value came from a repo as well / wasn't modified by the user
            {
                Name = result.Name;
                OnPropertyChanged(nameof(Name));
            }

        }
    }

    public class KnownUrl
    {
        public KnownUrl(string title, string url)
        {
            Title = title;
            Url = url;
        }

        public string Title { get; }
        public string Url { get; }

        public override string ToString() => Url;
    }
}
