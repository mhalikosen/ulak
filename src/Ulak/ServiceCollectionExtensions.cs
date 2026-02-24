using System.Reflection;
using System.Runtime.CompilerServices;
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

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static IServiceCollection AddUlak(this IServiceCollection services)
        => AddUlak(services, Assembly.GetCallingAssembly());

    public static IServiceCollection AddUlak(this IServiceCollection services, params Assembly[] assemblies)
    {
        services.TryAddScoped<ISender, Sender>();

        foreach (var assembly in assemblies)
        {
            RegisterHandlers(services, assembly);
        }

        return services;
    }

    public static IServiceCollection AddUlakBehavior(this IServiceCollection services, Type behaviorType)
    {
        if (behaviorType.IsGenericTypeDefinition)
        {
            services.AddScoped(typeof(IPipelineBehavior<,>), behaviorType);
        }
        else
        {
            var pipelineInterfaces = behaviorType.GetInterfaces()
                .Where(interfaceType =>
                    interfaceType.IsGenericType &&
                    interfaceType.GetGenericTypeDefinition() == typeof(IPipelineBehavior<,>));

            foreach (var pipelineInterface in pipelineInterfaces)
            {
                services.AddScoped(pipelineInterface, behaviorType);
            }
        }

        return services;
    }

    private static void RegisterHandlers(IServiceCollection services, Assembly assembly)
    {
        var concreteTypes = assembly.GetTypes()
            .Where(type => type is { IsAbstract: false, IsInterface: false, IsGenericTypeDefinition: false });

        foreach (var concreteType in concreteTypes)
        {
            var implementedInterfaces = concreteType.GetInterfaces()
                .Where(interfaceType =>
                    interfaceType.IsGenericType &&
                    HandlerInterfaces.Contains(interfaceType.GetGenericTypeDefinition()));

            foreach (var handlerInterface in implementedInterfaces)
            {
                services.AddScoped(handlerInterface, concreteType);
            }
        }
    }
}
