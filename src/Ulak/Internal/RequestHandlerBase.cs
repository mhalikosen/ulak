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
        PipelineStep<TResponse> handlerDelegate,
        CancellationToken cancellationToken)
        where TRequest : IRequest<TResponse>
    {
        var behaviorArray = behaviors.ToArray();

        if (behaviorArray.Length == 0)
            return handlerDelegate();

        var nextHandler = handlerDelegate;

        for (var i = behaviorArray.Length - 1; i >= 0; i--)
        {
            var behavior = behaviorArray[i];
            var currentHandler = nextHandler;
            nextHandler = () => behavior.HandleAsync(request, currentHandler, cancellationToken);
        }

        return nextHandler();
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
        var handler = serviceProvider.GetService<ICommandHandler<TCommand, TResponse>>()
            ?? throw new InvalidOperationException(
                $"No handler registered for command '{typeof(TCommand).Name}'. " +
                $"Register an ICommandHandler<{typeof(TCommand).Name}, {typeof(TResponse).Name}> implementation.");

        var behaviors = serviceProvider.GetServices<IPipelineBehavior<TCommand, TResponse>>();

        PipelineStep<TResponse> handlerDelegate = ()
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
        var handler = serviceProvider.GetService<ICommandHandler<TCommand>>()
            ?? throw new InvalidOperationException(
                $"No handler registered for command '{typeof(TCommand).Name}'. " +
                $"Register an ICommandHandler<{typeof(TCommand).Name}> implementation.");

        var behaviors = serviceProvider.GetServices<IPipelineBehavior<TCommand, Unit>>();

        PipelineStep<Unit> handlerDelegate = () =>
        {
            var task = handler.HandleAsync((TCommand)request, cancellationToken);

            if (task.IsCompletedSuccessfully)
                return Unit.Task;

            return AwaitAndReturnUnitAsync(task);
        };

        return BuildPipeline((TCommand)request, behaviors, handlerDelegate, cancellationToken);
    }

    private static async Task<Unit> AwaitAndReturnUnitAsync(Task task)
    {
        await task.ConfigureAwait(false);
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
        var handler = serviceProvider.GetService<IQueryHandler<TQuery, TResponse>>()
            ?? throw new InvalidOperationException(
                $"No handler registered for query '{typeof(TQuery).Name}'. " +
                $"Register an IQueryHandler<{typeof(TQuery).Name}, {typeof(TResponse).Name}> implementation.");

        var behaviors = serviceProvider.GetServices<IPipelineBehavior<TQuery, TResponse>>();

        PipelineStep<TResponse> handlerDelegate = ()
            => handler.HandleAsync((TQuery)request, cancellationToken);

        return BuildPipeline((TQuery)request, behaviors, handlerDelegate, cancellationToken);
    }
}