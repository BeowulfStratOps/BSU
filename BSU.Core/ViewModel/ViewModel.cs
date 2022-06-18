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

        internal ViewModel(IServiceProvider services)
        {
            _serviceProvider = services;
            _dispatcher = services.Get<IDispatcher>();
            var eventManager = services.Get<IEventManager>();
            var asyncVoidExecutor = services.Get<IAsyncVoidExecutor>();
            
            eventManager.Subscribe<ErrorEvent>(AddError);
            eventManager.Subscribe<NotificationEvent>(AddNotification);
            
            RepoPage = new RepositoriesPage(_serviceProvider, this);
            StoragePage = new StoragePage(_serviceProvider, this);
            Navigator = new Navigator(RepoPage);
            Settings = new DelegateCommand(() => asyncVoidExecutor.Execute(OpenSettings));
        }

        private async Task OpenSettings()
        {
            var interactionService = _serviceProvider.Get<IInteractionService>();
            var model = _serviceProvider.Get<IModel>();
            var themeService = _serviceProvider.Get<IThemeService>();
            var vm = new GlobalSettings(model.GetSettings(), themeService);
            if (await interactionService.GlobalSettings(vm))
                model.SetSettings(vm.ToModelSettings());
            else
                themeService.SetTheme(model.GetSettings().Theme!); // reset theme
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

        async Task<IModelStorage?> IViewModelService.AddStorage()
        {
            return await StoragePage.DoAddStorage();
        }

        public ObservableCollection<Notification> Notifications { get; } = new();
        public readonly DelegateCommand Settings;
        private readonly IServiceProvider _serviceProvider;

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
            Task.Delay(4500).ContinueWith(_ =>
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
