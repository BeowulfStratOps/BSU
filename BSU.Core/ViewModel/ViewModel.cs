using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using BSU.Core.Model;
using BSU.Core.ViewModel.Util;

namespace BSU.Core.ViewModel
{
    public class ViewModel : ObservableBase, IViewModelService, IErrorPresenter
    {
        private object _content;
        public object Content
        {
            get => _content;
            private set
            {
                if (_content == value) return;
                _content = value;
                OnPropertyChanged();
            }
        }

        private readonly RepositoriesPage _repoPage;
        private readonly StoragePage _storagePage;

        internal ViewModel(IModel model)
        {
            model.ConnectErrorPresenter(this);
            _repoPage = new RepositoriesPage(model, this);
            _storagePage = new StoragePage(model, this);
            Content = _repoPage;
        }

        public async Task Load()
        {
            await Task.WhenAll(_repoPage.Load(), _storagePage.Load());
        }

        public async Task Update()
        {
            try
            {
                await Task.WhenAll(_repoPage.Update(), _storagePage.Update());
            }
            catch (OperationCanceledException)
            {
                // happens if something changes in the model. view model will call updated again after whatever cause the change is done
            }
        }

        private readonly Stack<object> _navigationStack = new();

        public void NavigateToRepositories()
        {
            _navigationStack.Push(Content);
            Content = _repoPage;
        }

        public void NavigateToStorages()
        {
            _navigationStack.Push(Content);
            Content = _storagePage;
        }

        public void NavigateToRepository(Repository repository)
        {
            _navigationStack.Push(Content);
            Content = repository;
        }

        public void NavigateBack()
        {
            Content = _navigationStack.Pop();
        }

        public IInteractionService InteractionService { get; set; }
        public IAsyncVoidExecutor AsyncVoidExecutor { get; } = new AsyncVoidExecutor();

        public ObservableCollection<DismissError> Errors { get; } = new();

        public void AddError(string error)
        {
            Errors.Add(new DismissError(error, de => Errors.Remove(de)));
        }

        public void Run()
        {
            AsyncVoidExecutor.Execute(Load);
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
