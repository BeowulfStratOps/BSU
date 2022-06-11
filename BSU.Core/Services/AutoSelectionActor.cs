using System;
using BSU.Core.Events;
using BSU.Core.Ioc;
using BSU.Core.Model;

namespace BSU.Core.Services;

internal class AutoSelectionActor
{
    private readonly IAutoSelectionService _autoSelectionService;

    public AutoSelectionActor(IServiceProvider serviceProvider, IModel model)
    {
        var eventManager = serviceProvider.Get<IEventManager>();
        _autoSelectionService = serviceProvider.Get<IAutoSelectionService>();
        eventManager.Subscribe<AnythingChangedEvent>(_ => OnAnyChange(model));
    }

    private void OnAnyChange(IModel model)
    {
        foreach (var mod in model.GetRepositoryMods())
        {
            var selection = _autoSelectionService.GetAutoSelection(model, mod);
            if (selection != null)
                mod.SetSelection(selection);
        }
    }
}
