using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BSU.Core.Model;
using BSU.Core.ViewModel.Util;

namespace BSU.Core.ViewModel
{
    public class ViewModel : ObservableBase, IViewModelService
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
    }
}
