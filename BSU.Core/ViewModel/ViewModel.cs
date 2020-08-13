using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using BSU.Core.Annotations;
using BSU.Core.JobManager;
using BSU.Core.Model;

namespace BSU.Core.ViewModel
{
    public class ViewModel : ViewModelClass
    {
        private readonly IActionQueue _dispatcher;
        private Model.Model Model { get; }
        public ObservableCollection<Repository> Repositories { get; } = new ObservableCollection<Repository>();
        public ObservableCollection<Storage> Storages { get; } = new ObservableCollection<Storage>();
        
        public ObservableCollection<Job> Jobs { get; } = new ObservableCollection<Job>();

        internal ViewModel(Model.Model model, IJobManager jobManager, IActionQueue dispatcher)
        {
            _dispatcher = dispatcher;
            Model = model;
            model.RepositoryAdded += repository => Repositories.Add(new Repository(repository, this, model));
            model.StorageAdded += storage =>
            {
                Storages.Add(new Storage(storage, this));
                foreach (var repository in Repositories)
                {
                    foreach (var mod in repository.Mods)
                    {
                        mod.AddStorage(storage);
                    }
                }
            };
            jobManager.JobAdded += job =>
            {
                _dispatcher.EnQueueAction(() => JobAdded(job));
                job.OnFinished += () => _dispatcher.EnQueueAction(() => JobFinished(job));
            };

        }

        private void JobAdded(IJob job)
        {
            Jobs.Add(new Job(job));
        }
        
        private void JobFinished(IJob job)
        {
            var uiJob = Jobs.SingleOrDefault(j => j.BackingJob == job);
            Jobs.Remove(uiJob);
        }
        
        public void AddRepository(string type, string url, string name)
        {
            Model.AddRepository(type, url, name);
        }
        
        public void AddStorage(string type, string path, string name)
        {
            Model.AddStorage(type, new DirectoryInfo(path), name);
        }
    }
}
