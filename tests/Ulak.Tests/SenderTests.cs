using Microsoft.Extensions.DependencyInjection;

namespace Ulak.Tests;

public class SenderTests
{
    private static ServiceProvider CreateProvider()
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

        var result = await sender.SendAsync<Unit>(new CreateOrder("Widget", 5), TestContext.Current.CancellationToken);

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

        var id = await sender.SendAsync(new CreateOrderWithId("Widget"), TestContext.Current.CancellationToken);

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

        var result = await sender.SendAsync(new GetOrder(orderId), TestContext.Current.CancellationToken);

        Assert.Equal(orderId, result.Id);
        Assert.Equal("Test Product", result.Product);
    }

    [Fact]
    public async Task SendAsync_NullRequest_ThrowsArgumentNullException()
    {
        using var provider = CreateProvider();
        var sender = provider.GetRequiredService<ISender>();

        await Assert.ThrowsAsync<ArgumentNullException>(()
            => sender.SendAsync<Unit>(null!, TestContext.Current.CancellationToken));
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

        var result = await sender.SendAsync(new CommandNeedingDep("test"), TestContext.Current.CancellationToken);

        Assert.Equal("processed:test", result);
    }

    [Fact]
    public async Task SendAsync_MissingHandler_ThrowsDescriptiveError()
    {
        using var provider = CreateProvider();
        var sender = provider.GetRequiredService<ISender>();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(()
            => sender.SendAsync(new UnregisteredCommand(), TestContext.Current.CancellationToken));

        Assert.Contains("No handler registered for command", exception.Message);
        Assert.Contains("UnregisteredCommand", exception.Message);
    }

    [Fact]
    public async Task SendAsync_NonGenericVoidCommand_ExecutesHandler()
    {
        using var provider = CreateProvider();
        using var scope = provider.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();

        await sender.SendAsync(new CreateOrder("Widget", 5), TestContext.Current.CancellationToken);

        var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<CreateOrder>>();
        Assert.True(((CreateOrderHandler)handler).WasHandled);
    }

    [Fact]
    public async Task SendAsync_NonGenericNullCommand_ThrowsArgumentNullException()
    {
        using var provider = CreateProvider();
        var sender = provider.GetRequiredService<ISender>();

        await Assert.ThrowsAsync<ArgumentNullException>(()
            => sender.SendAsync((ICommand)null!, TestContext.Current.CancellationToken));
    }
}
