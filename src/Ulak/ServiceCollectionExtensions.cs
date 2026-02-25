using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Ulak.Internal;

namespace Ulak;

public static class ServiceCollectionExtensions
{
    private static readonly Type[] HandlerInterfaces =
    [
        typeof(ICommandHandler<>),
        typeof(ICommandHandler<,>),
        typeof(IQueryHandler<,>)
    ];

    public static IServiceCollection AddUlak(this IServiceCollection services, params Assembly[] assemblies)
    {
        ArgumentNullException.ThrowIfNull(assemblies);
        ArgumentOutOfRangeException.ThrowIfZero(assemblies.Length);

        services.TryAddScoped<ISender, Sender>();

        foreach (var assembly in assemblies)
        {
            RegisterHandlers(services, assembly);
        }

        return services;
    }

    public static IServiceCollection AddUlakBehavior(this IServiceCollection services, Type behaviorType)
    {
        ArgumentNullException.ThrowIfNull(behaviorType);

        if (behaviorType.IsGenericTypeDefinition)
        {
            var implementsPipelineBehavior = behaviorType.GetInterfaces()
                .Any(interfaceType =>
                    interfaceType.IsGenericType &&
                    interfaceType.GetGenericTypeDefinition() == typeof(IPipelineBehavior<,>));

            if (!implementsPipelineBehavior)
            {
                throw new ArgumentException(
                    $"Type '{behaviorType.Name}' must implement IPipelineBehavior<TRequest, TResponse>.",
                    nameof(behaviorType));
            }

            var alreadyRegistered = services.Any(descriptor =>
                descriptor.ServiceType == typeof(IPipelineBehavior<,>) &&
                descriptor.ImplementationType == behaviorType);

            if (!alreadyRegistered)
            {
                services.AddScoped(typeof(IPipelineBehavior<,>), behaviorType);
            }
        }
        else
        {
            var pipelineInterfaces = behaviorType.GetInterfaces()
                .Where(interfaceType =>
                    interfaceType.IsGenericType &&
                    interfaceType.GetGenericTypeDefinition() == typeof(IPipelineBehavior<,>))
                .ToArray();

            if (pipelineInterfaces.Length == 0)
            {
                throw new ArgumentException(
                    $"Type '{behaviorType.Name}' must implement IPipelineBehavior<TRequest, TResponse>.",
                    nameof(behaviorType));
            }

            foreach (var pipelineInterface in pipelineInterfaces)
            {
                var alreadyRegistered = services.Any(descriptor =>
                    descriptor.ServiceType == pipelineInterface &&
                    descriptor.ImplementationType == behaviorType);

                if (!alreadyRegistered)
                {
                    services.AddScoped(pipelineInterface, behaviorType);
                }
            }
        }

        return services;
    }

    private static void RegisterHandlers(IServiceCollection services, Assembly assembly)
    {
        Type[] types;
        try
        {
            types = assembly.GetExportedTypes();
        }
        catch (ReflectionTypeLoadException exception)
        {
            types = exception.Types.OfType<Type>().ToArray();

            foreach (var loaderException in exception.LoaderExceptions.OfType<Exception>())
            {
                System.Diagnostics.Debug.WriteLine(
                    $"Ulak: Failed to load type from assembly '{assembly.FullName}': {loaderException.Message}");
            }
        }

        var concreteTypes = types
            .Where(type => type is { IsAbstract: false, IsInterface: false, IsGenericTypeDefinition: false });

        foreach (var concreteType in concreteTypes)
        {
            var implementedInterfaces = concreteType.GetInterfaces()
                .Where(interfaceType =>
                    interfaceType.IsGenericType &&
                    HandlerInterfaces.Contains(interfaceType.GetGenericTypeDefinition()));

            foreach (var handlerInterface in implementedInterfaces)
            {
                services.TryAddScoped(handlerInterface, concreteType);
            }
        }
    }
}
