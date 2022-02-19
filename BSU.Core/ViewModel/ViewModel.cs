using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using BSU.Core.Concurrency;
using BSU.Core.Events;
using BSU.Core.Ioc;
using BSU.Core.Model;
using BSU.Core.ViewModel.Util;

namespace BSU.Core.ViewModel
{
    public class ViewModel : ObservableBase, IViewModelService
    {
        internal readonly RepositoriesPage RepoPage;
        internal readonly StoragePage StoragePage;
        private readonly IDispatcher _dispatcher;

        internal ViewModel(ServiceProvider services)
        {
            var eventManager = services.Get<IEventManager>();
            eventManager.Subscribe<ErrorEvent>(AddError);
            eventManager.Subscribe<NotificationEvent>(AddNotification);
            services.Add<IViewModelService>(this);
            _dispatcher = services.Get<IDispatcher>();
            RepoPage = new RepositoriesPage(services);
            StoragePage = new StoragePage(services);
            Navigator = new Navigator(RepoPage);
        }

        public Navigator Navigator { get; }

        public void NavigateToStorages()
        {
            Navigator.To(StoragePage);
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
            return StoragePage.DoAddStorage(allowSteam);
        }

        Repository IViewModelService.FindVmRepo(IModelRepository repo)
        {
            return RepoPage.Repositories.Single(r => r.ModelRepository == repo);
        }

        public ObservableCollection<Notification> Notifications { get; } = new();

        private void AddError(ErrorEvent error)
        {
            if (Notifications.Any(n => n is DismissError de && de.Text == error.Message))
                return; // Prevent error spam
            Notifications.Add(new DismissError(error.Message, RemoveNotification));
        }

        private void AddNotification(NotificationEvent evt)
        {
            Notifications.Add(new TimedNotification(evt.Message, RemoveNotification, _dispatcher));
        }

        private void RemoveNotification(Notification notification) => Notifications.Remove(notification);
    }

    public abstract class Notification
    {
    }

    public class TimedNotification : Notification
    {
        public string Text { get; }

        public TimedNotification(string text, Action<TimedNotification> remove, IDispatcher dispatcher)
        {
            Text = text;
            Task.Delay(5000).ContinueWith(_ =>
            {
                dispatcher.ExecuteSynchronized(() => remove(this));
            });
        }
    }

    public class DismissError : Notification
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
