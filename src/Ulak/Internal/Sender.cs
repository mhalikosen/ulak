using System.Collections.Concurrent;
using System.Reflection;

namespace Ulak.Internal;

internal sealed class Sender(IServiceProvider serviceProvider) : ISender
{
    private static readonly ConcurrentDictionary<Type, Lazy<object>> HandlerWrappers = new();

    public Task<TResponse> SendAsync<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var wrapper = (RequestHandlerBase<TResponse>)HandlerWrappers.GetOrAdd(
            request.GetType(),
            requestType => new Lazy<object>(() => CreateWrapper<TResponse>(requestType))).Value;

        return wrapper.HandleAsync(request, serviceProvider, cancellationToken);
    }

    public async Task SendAsync(ICommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        await SendAsync<Unit>(command, cancellationToken);
    }

    private static object CreateWrapper<TResponse>(Type requestType)
    {
        var responseType = typeof(TResponse);
        var interfaces = requestType.GetInterfaces();

        // Check if it's a void command (ICommand -> Unit)
        if (responseType == typeof(Unit) && typeof(ICommand).IsAssignableFrom(requestType))
        {
            var wrapperType = typeof(VoidCommandHandlerWrapper<>).MakeGenericType(requestType);
            return CreateWrapperInstance(wrapperType, requestType);
        }

        // Check if it's a command with response
        var commandInterface = interfaces
            .FirstOrDefault(interfaceType => interfaceType.IsGenericType && interfaceType.GetGenericTypeDefinition() == typeof(ICommand<>));

        if (commandInterface is not null)
        {
            var wrapperType = typeof(CommandHandlerWrapper<,>).MakeGenericType(requestType, responseType);
            return CreateWrapperInstance(wrapperType, requestType);
        }

        // Check if it's a query
        var queryInterface = interfaces
            .FirstOrDefault(interfaceType => interfaceType.IsGenericType && interfaceType.GetGenericTypeDefinition() == typeof(IQuery<>));

        if (queryInterface is not null)
        {
            var wrapperType = typeof(QueryHandlerWrapper<,>).MakeGenericType(requestType, responseType);
            return CreateWrapperInstance(wrapperType, requestType);
        }

        throw new InvalidOperationException(
            $"No handler wrapper could be created for request type {requestType.Name}. " +
            $"Ensure it implements ICommand, ICommand<TResponse>, or IQuery<TResponse>.");
    }

    private static object CreateWrapperInstance(Type wrapperType, Type requestType)
    {
        var constructor = wrapperType.GetConstructor(Type.EmptyTypes);

        if (constructor is null)
        {
            throw new InvalidOperationException(
                $"Handler wrapper type '{wrapperType.Name}' for request '{requestType.Name}' does not have a parameterless constructor.");
        }

        return constructor.Invoke(null);
    }
}
