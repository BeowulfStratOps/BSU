using System;
using BSU.Core.Events;
using BSU.Core.Ioc;
using BSU.Core.Model;

namespace BSU.Core.Services;

public class RepositoryStateActor
{
    public RepositoryStateActor(IServiceProvider serviceProvider)
    {
        var eventManager = serviceProvider.Get<IEventManager>();
        var model = serviceProvider.Get<IModel>();
        eventManager.Subscribe<AnythingChangedEvent>(_ =>
        {
            foreach (var repository in model.GetRepositories())
            {
                eventManager.Publish(new CalculatedStateChangedEvent(repository));
            }
        });
    }
}
