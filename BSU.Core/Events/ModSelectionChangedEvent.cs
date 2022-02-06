using BSU.Core.Model;

namespace BSU.Core.Events;

internal record ModSelectionChangedEvent(IModelRepositoryMod RepositoryMod, ModSelection OldSelection, ModSelection NewSelection);
