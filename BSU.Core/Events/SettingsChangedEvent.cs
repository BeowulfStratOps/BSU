using BSU.Core.Model;

namespace BSU.Core.Events;

internal record SettingsChangedEvent(IModelRepository Repository);
