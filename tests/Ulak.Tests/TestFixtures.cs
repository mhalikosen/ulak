namespace Ulak.Tests;

// Sender test fixtures

public record CreateOrder(string Product, int Quantity) : ICommand;

public record CreateOrderWithId(string Product) : ICommand<Guid>;

public record GetOrder(Guid Id) : IQuery<OrderDto>;

public record OrderDto(Guid Id, string Product);

public record UnregisteredCommand : ICommand;

public record CancellableCommand : ICommand;

public record CommandNeedingDep(string Value) : ICommand<string>;

public class CreateOrderHandler : ICommandHandler<CreateOrder>
{
    public bool WasHandled { get; private set; }

    public Task HandleAsync(CreateOrder command, CancellationToken cancellationToken)
    {
        WasHandled = true;
        return Task.CompletedTask;
    }
}

public class CreateOrderWithIdHandler : ICommandHandler<CreateOrderWithId, Guid>
{
    public Guid GeneratedId { get; private set; }

    public Task<Guid> HandleAsync(CreateOrderWithId command, CancellationToken cancellationToken)
    {
        GeneratedId = Guid.NewGuid();
        return Task.FromResult(GeneratedId);
    }
}

public class GetOrderHandler : IQueryHandler<GetOrder, OrderDto>
{
    public Task<OrderDto> HandleAsync(GetOrder query, CancellationToken cancellationToken)
    {
        return Task.FromResult(new OrderDto(query.Id, "Test Product"));
    }
}

public class CancellableCommandHandler : ICommandHandler<CancellableCommand>
{
    public Task HandleAsync(CancellableCommand command, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.CompletedTask;
    }
}

public class ExternalService
{
    public string Prefix { get; } = "processed";
}

public class CommandNeedingDepHandler(ExternalService externalService) : ICommandHandler<CommandNeedingDep, string>
{
    public Task<string> HandleAsync(CommandNeedingDep command, CancellationToken cancellationToken)
    {
        return Task.FromResult($"{externalService.Prefix}:{command.Value}");
    }
}

// Pipeline behavior test fixtures

public record PingCommand(string Message) : ICommand<string>;

public record VoidPingCommand(string Message) : ICommand;

public class PingHandler : ICommandHandler<PingCommand, string>
{
    public Task<string> HandleAsync(PingCommand command, CancellationToken cancellationToken)
    {
        return Task.FromResult(command.Message);
    }
}

public class VoidPingHandler : ICommandHandler<VoidPingCommand>
{
    public Task HandleAsync(VoidPingCommand command, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}

public class UpperCaseBehavior : IPipelineBehavior<PingCommand, string>
{
    public async Task<string> HandleAsync(PingCommand request, PipelineStep<string> nextHandler, CancellationToken cancellationToken)
    {
        var result = await nextHandler();
        return result.ToUpperInvariant();
    }
}

public class WrapBehavior : IPipelineBehavior<PingCommand, string>
{
    public async Task<string> HandleAsync(PingCommand request, PipelineStep<string> nextHandler, CancellationToken cancellationToken)
    {
        var result = await nextHandler();
        return $"[{result}]";
    }
}

public class ThrowingBehavior : IPipelineBehavior<PingCommand, string>
{
    public Task<string> HandleAsync(PingCommand request, PipelineStep<string> nextHandler, CancellationToken cancellationToken)
    {
        throw new InvalidOperationException("Pipeline error");
    }
}

public class ExecutionTracker
{
    public List<string> Log { get; } = [];
}

public class OrderTrackingBehavior<TRequest, TResponse>(ExecutionTracker tracker) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> HandleAsync(TRequest request, PipelineStep<TResponse> nextHandler, CancellationToken cancellationToken)
    {
        tracker.Log.Add($"Before:{typeof(TRequest).Name}");
        var result = await nextHandler();
        tracker.Log.Add($"After:{typeof(TRequest).Name}");
        return result;
    }
}