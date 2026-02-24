using Microsoft.Extensions.DependencyInjection;

namespace Ulak.Tests;

public record CreateOrder(string Product, int Quantity) : ICommand;

public record CreateOrderWithId(string Product) : ICommand<Guid>;

public record GetOrder(Guid Id) : IQuery<OrderDto>;

public record OrderDto(Guid Id, string Product);

public class CreateOrderHandler : ICommandHandler<CreateOrder>
{
    public static bool WasHandled { get; set; }

    public Task HandleAsync(CreateOrder command, CancellationToken cancellationToken)
    {
        WasHandled = true;
        return Task.CompletedTask;
    }
}

public class CreateOrderWithIdHandler : ICommandHandler<CreateOrderWithId, Guid>
{
    public static Guid GeneratedId { get; set; }

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

public class SenderTests
{
    private ISender CreateSender()
    {
        var services = new ServiceCollection();
        services.AddUlak(typeof(SenderTests).Assembly);
        var provider = services.BuildServiceProvider();
        return provider.GetRequiredService<ISender>();
    }

    [Fact]
    public async Task SendAsync_VoidCommand_ExecutesHandler()
    {
        CreateOrderHandler.WasHandled = false;
        var sender = CreateSender();

        var result = await sender.SendAsync(new CreateOrder("Widget", 5));

        Assert.Equal(Unit.Value, result);
        Assert.True(CreateOrderHandler.WasHandled);
    }

    [Fact]
    public async Task SendAsync_CommandWithResponse_ReturnsResponse()
    {
        var sender = CreateSender();

        var id = await sender.SendAsync(new CreateOrderWithId("Widget"));

        Assert.NotEqual(Guid.Empty, id);
        Assert.Equal(CreateOrderWithIdHandler.GeneratedId, id);
    }

    [Fact]
    public async Task SendAsync_Query_ReturnsResponse()
    {
        var sender = CreateSender();
        var orderId = Guid.NewGuid();

        var result = await sender.SendAsync(new GetOrder(orderId));

        Assert.Equal(orderId, result.Id);
        Assert.Equal("Test Product", result.Product);
    }

    [Fact]
    public async Task SendAsync_NullRequest_ThrowsArgumentNullException()
    {
        var sender = CreateSender();

        await Assert.ThrowsAsync<ArgumentNullException>(()
            => sender.SendAsync<Unit>(null!));
    }
}
