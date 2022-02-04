namespace BSU.Core.Events;

public record AnythingChangedEvent;
// currently includes:
//  add storage
//  delete storage
//  storage state change
//  added storage mod
//  storage mod state change
//  add repo
//  delete repo
//  repo state change
//  repo mod state change
//  repo mod selection changed  (selection is only target. not target type / action type)
//  repo mod download identifier changed

// TODO: have a good think about how to split this up...
