using Microsoft.Extensions.DependencyInjection;

namespace Ulak.Tests;

public class RegistrationTests
{
    [Fact]
    public void AddUlakRegistersISender()
    {
        var services = new ServiceCollection();
        services.AddUlak();
        using var provider = services.BuildServiceProvider();

        var sender = provider.GetService<ISender>();

        Assert.NotNull(sender);
    }

    [Fact]
    public void AddUlakRegistersVoidCommandHandler()
    {
        var services = new ServiceCollection();
        services.AddUlak();
        using var provider = services.BuildServiceProvider();

        var handler = provider.GetService<ICommandHandler<CreateOrder>>();

        Assert.NotNull(handler);
        Assert.IsType<CreateOrderHandler>(handler);
    }

    [Fact]
    public void AddUlakRegistersCommandHandlerWithResponse()
    {
        var services = new ServiceCollection();
        services.AddUlak();
        using var provider = services.BuildServiceProvider();

        var handler = provider.GetService<ICommandHandler<CreateOrderWithId, Guid>>();

        Assert.NotNull(handler);
        Assert.IsType<CreateOrderWithIdHandler>(handler);
    }

    [Fact]
    public void AddUlakRegistersQueryHandler()
    {
        var services = new ServiceCollection();
        services.AddUlak();
        using var provider = services.BuildServiceProvider();

        var handler = provider.GetService<IQueryHandler<GetOrder, OrderDto>>();

        Assert.NotNull(handler);
        Assert.IsType<GetOrderHandler>(handler);
    }

    [Fact]
    public void AddUlakThrowsArgumentNullExceptionForNullConfigure()
    {
        var services = new ServiceCollection();

        Assert.Throws<ArgumentNullException>(()
            => services.AddUlak(null!));
    }

    [Fact]
    public void AddUlakRegistersBehavior()
    {
        var services = new ServiceCollection();
        services.AddScoped<ExecutionTracker>();
        services.AddUlak(options =>
        {
            options.AddBehavior<OrderTrackingBehavior>();
        });
        using var provider = services.BuildServiceProvider();

        var behaviors = provider.GetServices<IPipelineBehavior>();

        Assert.Contains(behaviors, behavior => behavior is OrderTrackingBehavior);
    }
}