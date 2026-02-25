using Microsoft.Extensions.DependencyInjection;

namespace Ulak.Tests;

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

public class SenderTests
{
    private ServiceProvider CreateProvider()
    {
        var services = new ServiceCollection();
        services.AddUlak(typeof(SenderTests).Assembly);
        services.AddScoped<ExternalService>();
        return services.BuildServiceProvider();
    }

    [Fact]
    public async Task SendAsync_VoidCommand_ExecutesHandler()
    {
        using var provider = CreateProvider();
        using var scope = provider.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();

        var result = await sender.SendAsync(new CreateOrder("Widget", 5));

        Assert.Equal(Unit.Value, result);

        var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<CreateOrder>>();
        Assert.True(((CreateOrderHandler)handler).WasHandled);
    }

    [Fact]
    public async Task SendAsync_CommandWithResponse_ReturnsResponse()
    {
        using var provider = CreateProvider();
        using var scope = provider.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();

        var id = await sender.SendAsync(new CreateOrderWithId("Widget"));

        Assert.NotEqual(Guid.Empty, id);

        var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<CreateOrderWithId, Guid>>();
        Assert.Equal(((CreateOrderWithIdHandler)handler).GeneratedId, id);
    }

    [Fact]
    public async Task SendAsync_Query_ReturnsResponse()
    {
        using var provider = CreateProvider();
        var sender = provider.GetRequiredService<ISender>();
        var orderId = Guid.NewGuid();

        var result = await sender.SendAsync(new GetOrder(orderId));

        Assert.Equal(orderId, result.Id);
        Assert.Equal("Test Product", result.Product);
    }

    [Fact]
    public async Task SendAsync_NullRequest_ThrowsArgumentNullException()
    {
        using var provider = CreateProvider();
        var sender = provider.GetRequiredService<ISender>();

        await Assert.ThrowsAsync<ArgumentNullException>(()
            => sender.SendAsync<Unit>(null!));
    }

    [Fact]
    public async Task SendAsync_CancelledToken_ThrowsOperationCancelledException()
    {
        using var provider = CreateProvider();
        var sender = provider.GetRequiredService<ISender>();
        using var cancellationTokenSource = new CancellationTokenSource();
        await cancellationTokenSource.CancelAsync();

        await Assert.ThrowsAsync<OperationCanceledException>(()
            => sender.SendAsync(new CancellableCommand(), cancellationTokenSource.Token));
    }

    [Fact]
    public async Task SendAsync_HandlerWithDependency_ResolvesDependency()
    {
        using var provider = CreateProvider();
        var sender = provider.GetRequiredService<ISender>();

        var result = await sender.SendAsync(new CommandNeedingDep("test"));

        Assert.Equal("processed:test", result);
    }

    [Fact]
    public async Task SendAsync_MissingHandler_ThrowsDescriptiveError()
    {
        using var provider = CreateProvider();
        var sender = provider.GetRequiredService<ISender>();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(()
            => sender.SendAsync(new UnregisteredCommand()));

        Assert.Contains("No handler registered for command", exception.Message);
        Assert.Contains("UnregisteredCommand", exception.Message);
    }
}
