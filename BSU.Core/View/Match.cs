using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using BSU.Core.Annotations;
using BSU.Core.Model;

namespace BSU.Core.View
{
    public class Match : INotifyPropertyChanged
    {
        private string _action;

        internal Model.StorageMod Mod { get; private set; }

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

        internal Match(Model.StorageMod mod, RepositoryMod parent, ModAction modelAction)
        {
            Mod = mod;
            Action = modelAction.ToString();
            Parent = parent;
        }

        internal void Update(ModAction modelAction)
        {
            Action = modelAction.ToString();
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
                var update = Mod.PrepareUpdate(Parent.Mod);
                update.OnPrepared += update.Commit;
            }).Start();
        }
    }
}