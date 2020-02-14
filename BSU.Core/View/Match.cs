using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using BSU.Core.Annotations;
using BSU.Core.Model.Actions;

namespace BSU.Core.View
{
    public class Match : INotifyPropertyChanged
    {
        private string _action;
        public StorageTarget StorageTarget { get; }

        internal ModAction ModelAction { get; private set; }

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

        internal Match(StorageTarget target, RepositoryMod parent, ModAction modelAction)
        {
            StorageTarget = target;
            Action = modelAction.ToString();
            Parent = parent;
            ModelAction = modelAction;
        }

        internal void Update(ModAction modelAction)
        {
            Action = modelAction.ToString();
            ModelAction = modelAction;
        }
        
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
                var model = Parent.ViewModel.Model;
                model.DoUpdate(model.PrepareUpdate(new List<ModAction> {ModelAction}));
            }).Start();
        }
    }
}