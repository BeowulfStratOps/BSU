using System;
using BSU.Core.Events;
using BSU.Core.Ioc;
using BSU.Core.Model;

namespace BSU.Core.Services;

internal class RepositoryStateService : IRepositoryStateService
{
    private IModel? _model;
    private readonly IServiceProvider _serviceProvider;

    public RepositoryStateService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        var eventManager = serviceProvider.Get<IEventManager>();
        eventManager.Subscribe<AnythingChangedEvent>(_ =>
        {
            _model ??= serviceProvider.Get<IModel>();
            foreach (var repository in _model.GetRepositories())
            {
                // TODO: include the state?
                eventManager.Publish(new CalculatedStateChangedEvent(repository));
            }
        });
    }

    public CalculatedRepositoryStateEnum GetStateFor(IModelRepository repository)
    {
        _model ??= _serviceProvider.Get<IModel>();
        return CoreCalculation.GetRepositoryState(repository, _model.GetRepositoryMods());
    }
}

internal interface IRepositoryStateService
{
    CalculatedRepositoryStateEnum GetStateFor(IModelRepository repository);
}
