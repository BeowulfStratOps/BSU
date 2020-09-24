using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using BSU.Core.Annotations;
using BSU.Core.Model;
using BSU.Core.ViewModel.Util;

namespace BSU.Core.ViewModel
{
    public class Storage : ViewModelClass
    {
        private readonly IActionQueue _dispatcher;
        private readonly IModelStorage _storage;
        private readonly IModel _model;
        public string Name { get; }
        internal Model.Storage ModelStorage { get; }
        
        public bool IsLoading { get; }

        public ObservableCollection<StorageMod> Mods { get; } = new ObservableCollection<StorageMod>();
        
        public DelegateCommand Delete { get; }
        public InteractionRequest<MsgPopupContext, bool?> DeleteInteraction { get; } = new InteractionRequest<MsgPopupContext, bool?>();

        internal Storage(Model.Storage storage, IActionQueue dispatcher, IModel model)
        {
            Delete = new DelegateCommand(DoDelete);
            IsLoading = storage.Loading.IsActive();
            ModelStorage = storage;
            _dispatcher = dispatcher;
            _model = model;
            _storage = storage;
            Name = storage.Identifier;
            storage.ModAdded += mod => Mods.Add(new StorageMod(mod));
        }

        private void DoDelete()
        {
            // TODO: this doesn't look like it belongs here
            var text = $@"Removing storage {Name}. Do you want to delete the files?

Yes - Delete mods in on this storage
No - Keep mods
Cancel - Do not remove this storage";
            
            var context = new MsgPopupContext(text, "Remove Storage");
            _dispatcher.EnQueueAction(() =>
            {
                DeleteInteraction.Raise(context, b =>
                {
                    if (b == null) return;
                    _model.DeleteStorage(_storage, (bool) b);
                });
            });
        }
    }
}
