using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using BSU.Core.Annotations;
using BSU.Core.Model;
using BSU.Core.ViewModel.Util;

namespace BSU.Core.ViewModel
{
    public class Storage : ObservableBase
    {
        private readonly IModelStorage _storage;
        private readonly IModel _model;
        public string Name { get; }
        internal IModelStorage ModelStorage { get; }

        public ObservableCollection<StorageMod> Mods { get; } = new ObservableCollection<StorageMod>();

        public DelegateCommand Delete { get; }
        public InteractionRequest<MsgPopupContext, bool?> DeleteInteraction { get; } = new InteractionRequest<MsgPopupContext, bool?>();
        public Guid Identifier { get; }

        internal Storage(IModelStorage storage, IModel model)
        {
            Delete = new DelegateCommand(DoDelete);
            ModelStorage = storage;
            _model = model;
            Identifier = storage.Identifier;
            _storage = storage;
            Name = storage.Name;
        }

        internal async Task Load()
        {
            var mods = await ModelStorage.GetMods();
            foreach (var mod in mods)
            {
                Mods.Add(new StorageMod(mod));
            }
        }

        private async Task DoDelete()
        {
            // TODO: this doesn't look like it belongs here
            var text = $@"Removing storage {Name}. Do you want to delete the files?

Yes - Delete mods in on this storage
No - Keep mods
Cancel - Do not remove this storage";

            var context = new MsgPopupContext(text, "Remove Storage");
            var removeMods = await DeleteInteraction.Raise(context);
            if (removeMods == null) return;
            _model.DeleteStorage(_storage, (bool) removeMods);
        }
    }
}
