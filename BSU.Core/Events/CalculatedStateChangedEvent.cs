using BSU.Core.Model;

namespace BSU.Core.Events;

internal record CalculatedStateChangedEvent(IModelRepository Repository);
