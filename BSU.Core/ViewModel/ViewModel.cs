using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using BSU.Core.Events;
using BSU.Core.Ioc;
using BSU.Core.Model;
using BSU.Core.ViewModel.Util;

namespace BSU.Core.ViewModel
{
    public class ViewModel : ObservableBase, IViewModelService
    {
        private readonly RepositoriesPage _repoPage;
        private readonly StoragePage _storagePage;

        internal ViewModel(ServiceProvider services)
        {
            services.Get<IEventManager>().Subscribe<ErrorEvent>(AddError);
            services.Add<IViewModelService>(this);
            _repoPage = new RepositoriesPage(services);
            _storagePage = new StoragePage(services);
            Navigator = new Navigator(_repoPage);
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

        IModelStorage? IViewModelService.AddStorage(bool allowSteam)
        {
            return _storagePage.DoAddStorage(allowSteam);
        }

        Repository IViewModelService.FindVmRepo(IModelRepository repo)
        {
            return _repoPage.Repositories.Single(r => r.ModelRepository == repo);
        }

        public ObservableCollection<DismissError> Errors { get; } = new();

        private void AddError(ErrorEvent error)
        {
            Errors.Add(new DismissError(error.Message, de => Errors.Remove(de)));
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
