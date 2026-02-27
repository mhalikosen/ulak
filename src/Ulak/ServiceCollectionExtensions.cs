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

    public static IServiceCollection AddUlak(this IServiceCollection services)
    {
        services.TryAddScoped<ISender, Sender>();

        foreach (var assembly in GetApplicationAssemblies())
        {
            RegisterHandlers(services, assembly);
        }

        return services;
    }

    public static IServiceCollection AddUlak(this IServiceCollection services, Action<UlakOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        services.AddUlak();

        var options = new UlakOptions();
        configure(options);

        foreach (var behaviorType in options.BehaviorTypes)
        {
            services.AddScoped(typeof(IPipelineBehavior), behaviorType);
        }

        return services;
    }

    private static Assembly[] GetApplicationAssemblies()
    {
        return AppDomain.CurrentDomain.GetAssemblies()
            .Where(assembly => !IsFrameworkAssembly(assembly))
            .ToArray();
    }

    private static bool IsFrameworkAssembly(Assembly assembly)
    {
        if (assembly.IsDynamic) return true;

        var name = assembly.GetName().Name;
        if (name is null) return true;

        return name.StartsWith("System", StringComparison.Ordinal) ||
               name.StartsWith("Microsoft", StringComparison.Ordinal) ||
               name.StartsWith("mscorlib", StringComparison.Ordinal) ||
               name.StartsWith("netstandard", StringComparison.Ordinal);
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