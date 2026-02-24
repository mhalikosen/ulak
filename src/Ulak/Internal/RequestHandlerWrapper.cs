using Microsoft.Extensions.DependencyInjection;

namespace Ulak.Internal;

internal abstract class RequestHandlerBase<TResponse>
{
    public abstract Task<TResponse> HandleAsync(
        IRequest<TResponse> request,
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken);

    protected static Task<TResponse> BuildPipeline<TRequest>(
        TRequest request,
        IEnumerable<IPipelineBehavior<TRequest, TResponse>> behaviors,
        RequestHandlerDelegate<TResponse> handlerDelegate,
        CancellationToken cancellationToken)
        where TRequest : IRequest<TResponse>
    {
        var next = handlerDelegate;

        foreach (var behavior in behaviors.Reverse())
        {
            var current = next;
            next = () => behavior.HandleAsync(request, current, cancellationToken);
        }

        return next();
    }
}

internal sealed class CommandHandlerWrapper<TCommand, TResponse> : RequestHandlerBase<TResponse>
    where TCommand : ICommand<TResponse>
{
    public override Task<TResponse> HandleAsync(
        IRequest<TResponse> request,
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken)
    {
        var handler = serviceProvider.GetRequiredService<ICommandHandler<TCommand, TResponse>>();
        var behaviors = serviceProvider.GetServices<IPipelineBehavior<TCommand, TResponse>>();

        RequestHandlerDelegate<TResponse> handlerDelegate = ()
            => handler.HandleAsync((TCommand)request, cancellationToken);

        return BuildPipeline((TCommand)request, behaviors, handlerDelegate, cancellationToken);
    }
}

internal sealed class VoidCommandHandlerWrapper<TCommand> : RequestHandlerBase<Unit>
    where TCommand : ICommand
{
    public override Task<Unit> HandleAsync(
        IRequest<Unit> request,
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken)
    {
        var handler = serviceProvider.GetRequiredService<ICommandHandler<TCommand>>();
        var behaviors = serviceProvider.GetServices<IPipelineBehavior<TCommand, Unit>>();

        RequestHandlerDelegate<Unit> handlerDelegate = () =>
        {
            var task = handler.HandleAsync((TCommand)request, cancellationToken);

            if (task.IsCompletedSuccessfully)
                return Unit.Task;

            return AwaitAndReturnUnit(task);
        };

        return BuildPipeline((TCommand)request, behaviors, handlerDelegate, cancellationToken);
    }

    private static async Task<Unit> AwaitAndReturnUnit(Task task)
    {
        await task;
        return Unit.Value;
    }
}

internal sealed class QueryHandlerWrapper<TQuery, TResponse> : RequestHandlerBase<TResponse>
    where TQuery : IQuery<TResponse>
{
    public override Task<TResponse> HandleAsync(
        IRequest<TResponse> request,
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken)
    {
        var handler = serviceProvider.GetRequiredService<IQueryHandler<TQuery, TResponse>>();
        var behaviors = serviceProvider.GetServices<IPipelineBehavior<TQuery, TResponse>>();

        RequestHandlerDelegate<TResponse> handlerDelegate = ()
            => handler.HandleAsync((TQuery)request, cancellationToken);

        return BuildPipeline((TQuery)request, behaviors, handlerDelegate, cancellationToken);
    }
}
