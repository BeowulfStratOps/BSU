using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using BSU.Core.Model;
using BSU.Core.Services;
using BSU.Core.ViewModel.Util;

namespace BSU.Core.ViewModel
{
    public class ViewModel : ObservableBase, IViewModelService, IErrorPresenter
    {
        private readonly IModel _model;
        private object _content;

        private readonly RepositoriesPage _repoPage;
        private readonly StoragePage _storagePage;

        internal ViewModel(IModel model, IInteractionService interactionService)
        {
            _model = model;
            model.ConnectErrorPresenter(this);
            _repoPage = new RepositoriesPage(model, this);
            _storagePage = new StoragePage(model, this);
            _content = _repoPage;
            Navigator = new Navigator(_repoPage);
            InteractionService = interactionService;
        }

        public Navigator Navigator { get; }

        public void NavigateToStorages()
        {
            Navigator.To(_storagePage);
        }

        public void NavigateToRepository(Repository repository)
        {
            Navigator.To(repository);
        }

        public void NavigateBack()
        {
            Navigator.Back();
        }

        public IInteractionService InteractionService { get; set; }

        IModelStorage? IViewModelService.AddStorage(bool allowSteam)
        {
            return _storagePage.DoAddStorage(allowSteam);
        }

        Repository IViewModelService.FindVmRepo(IModelRepository repo)
        {
            return _repoPage.Repositories.Single(r => r.ModelRepository == repo);
        }

        public ObservableCollection<DismissError> Errors { get; } = new();

        public void AddError(string error)
        {
            Errors.Add(new DismissError(error, de => Errors.Remove(de)));
        }
    }

    public class DismissError
    {
        public string Text { get; }
        public ICommand Dismiss { get; }

        public DismissError(string text, Action<DismissError> dismiss)
        {
            Text = text;
            Dismiss = new DelegateCommand(() => dismiss(this));
        }
    }
}
