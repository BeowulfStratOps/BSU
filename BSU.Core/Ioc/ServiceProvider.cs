using System;
using System.Collections.Generic;

namespace BSU.Core.Ioc;

public class ServiceProvider : IServiceProvider
{
    private readonly Dictionary<Type, object> _services = new();

    public void Add<T>(T instance) where T : class => _services.Add(typeof(T), instance);
    public object GetService(Type serviceType) => _services[serviceType];
}

public static class ServiceProviderExtension
{
    public static T Get<T>(this IServiceProvider serviceProvider)
    {
        var result = serviceProvider.GetService(typeof(T));
        return (T)result!;
    }
}
