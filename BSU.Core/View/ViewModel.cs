using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
using BSU.Core.Annotations;
using BSU.Core.Services;
using BSU.CoreCommon;

namespace BSU.Core.View
{
    public class ViewModel : INotifyPropertyChanged
    {
        internal Model.Model Model { get; }
        private readonly Core _core;
        public ObservableCollection<Repository> Repositories { get; } = new ObservableCollection<Repository>();
        public ObservableCollection<Storage> Storages { get; } = new ObservableCollection<Storage>();
        internal static Action<Action> UiDo;
        
        public ObservableCollection<Job> Jobs { get; } = new ObservableCollection<Job>();
        public ObservableCollection<string> Logs { get; } = new ObservableCollection<string>();

        internal ViewModel(Core core, Action<Action> uiDo, Model.Model model)
        {
            Model = model;
            _core = core;
            UiDo = uiDo;
            model.RepositoryAdded += repository => uiDo(() => Repositories.Add(new Repository(repository, this)));
            model.StorageAdded += storage => uiDo(() =>
            {
                Storages.Add(new Storage(storage, this));
                foreach (var repository in Repositories)
                {
                    foreach (var mod in repository.Mods)
                    {
                        mod.AddStorage(storage);
                    }
                }
            });
            ServiceProvider.JobManager.JobAdded += job =>
            {
                lock (this)
                {
                    UiDo(() => Logs.Add("Started " + job.GetTitle()));
                    UiDo(() => Jobs.Add(new Job(job)));
                }
            };
            ServiceProvider.JobManager.JobRemoved += job =>
            {
                lock (this)
                {
                    UiDo(() => Logs.Add("Ended " + job.GetTitle()));
                    var uiJob = Jobs.SingleOrDefault(j => j.BackingJob == job);
                    UiDo(() => Jobs.Remove(uiJob));
                }
            };

        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
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
