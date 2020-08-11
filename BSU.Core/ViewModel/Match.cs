using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using BSU.Core.Annotations;
using BSU.Core.Model;

namespace BSU.Core.ViewModel
{
    public class Match : INotifyPropertyChanged
    {
        private string _action;

        internal IModelStorageMod Mod { get; private set; }

        public string Action
        {
            get => _action;
            internal set
            {
                if (value == _action) return;
                _action = value;
                OnPropertyChanged();
            }
        }

        public RepositoryMod Parent { get; }

        internal Match(IModelStorageMod mod, RepositoryMod parent, ModAction modelAction)
        {
            Mod = mod;
            Action = modelAction.ActionType.ToString();
            modelAction.Updated += () =>
            {
                Action = modelAction.ActionType.ToString();
            };

            foreach (var conflict in modelAction.Conflicts)
            {
                Conflicts.Add("???");
            }

            modelAction.ConflictAdded += action => Conflicts.Add("???");
            
            modelAction.ConflictRemoved += action => Conflicts.Remove("???");
            Parent = parent;
        }

        public ObservableCollection<string> Conflicts { get; } = new ObservableCollection<string>();
        
        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void DoUpdate()
        {
            new Thread(() =>
            {
                var update = Mod.PrepareUpdate(Parent.Mod.Implementation, Parent.Mod.AsUpdateTarget, e => { });
                update.OnPrepared += update.Commit;
            }).Start();
        }
    }
}