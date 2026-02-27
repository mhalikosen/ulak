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

public class UpperCaseBehavior : IPipelineBehavior
{
    public async Task<TResponse> HandleAsync<TRequest, TResponse>(TRequest request, NextStep<TResponse> nextHandler, CancellationToken cancellationToken)
        where TRequest : IRequest<TResponse>
    {
        var result = await nextHandler();
        if (result is string stringResult)
            return (TResponse)(object)stringResult.ToUpperInvariant();
        return result;
    }
}

public class WrapBehavior : IPipelineBehavior
{
    public async Task<TResponse> HandleAsync<TRequest, TResponse>(TRequest request, NextStep<TResponse> nextHandler, CancellationToken cancellationToken)
        where TRequest : IRequest<TResponse>
    {
        var result = await nextHandler();
        if (result is string stringResult)
            return (TResponse)(object)$"[{stringResult}]";
        return result;
    }
}

public class ThrowingBehavior : IPipelineBehavior
{
    public Task<TResponse> HandleAsync<TRequest, TResponse>(TRequest request, NextStep<TResponse> nextHandler, CancellationToken cancellationToken)
        where TRequest : IRequest<TResponse>
    {
        throw new InvalidOperationException("Pipeline error");
    }
}

public class ExecutionTracker
{
    public List<string> Log { get; } = [];
}

public class OrderTrackingBehavior(ExecutionTracker tracker) : IPipelineBehavior
{
    public async Task<TResponse> HandleAsync<TRequest, TResponse>(TRequest request, NextStep<TResponse> nextHandler, CancellationToken cancellationToken)
        where TRequest : IRequest<TResponse>
    {
        tracker.Log.Add($"Before:{typeof(TRequest).Name}");
        var result = await nextHandler();
        tracker.Log.Add($"After:{typeof(TRequest).Name}");
        return result;
    }
}