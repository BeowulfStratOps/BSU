using System;
using BSU.Core.Events;
using BSU.Core.Ioc;
using BSU.Core.Model;

namespace BSU.Core.Services;

internal class AutoSelectionActor
{
    public AutoSelectionActor(IServiceProvider serviceProvider, IModel model)
    {
        var eventManager = serviceProvider.Get<IEventManager>();
        eventManager.Subscribe<AnythingChangedEvent>(_ => OnAnyChange(model));
    }

    private static void OnAnyChange(IModel model)
    {
        foreach (var mod in model.GetRepositoryMods())
        {
            var selection = AutoSelectorCalculation.GetAutoSelection(model, mod);
            if (selection != null)
                mod.SetSelection(selection);
        }
    }
}
