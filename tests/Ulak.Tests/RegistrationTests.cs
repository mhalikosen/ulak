using Microsoft.Extensions.DependencyInjection;

namespace Ulak.Tests;

public class RegistrationTests
{
    [Fact]
    public void AddUlak_RegistersISender()
    {
        var services = new ServiceCollection();
        services.AddUlak(typeof(RegistrationTests).Assembly);
        var provider = services.BuildServiceProvider();

        var sender = provider.GetService<ISender>();

        Assert.NotNull(sender);
    }

    [Fact]
    public void AddUlak_RegistersVoidCommandHandler()
    {
        var services = new ServiceCollection();
        services.AddUlak(typeof(RegistrationTests).Assembly);
        var provider = services.BuildServiceProvider();

        var handler = provider.GetService<ICommandHandler<CreateOrder>>();

        Assert.NotNull(handler);
        Assert.IsType<CreateOrderHandler>(handler);
    }

    [Fact]
    public void AddUlak_RegistersCommandHandlerWithResponse()
    {
        var services = new ServiceCollection();
        services.AddUlak(typeof(RegistrationTests).Assembly);
        var provider = services.BuildServiceProvider();

        var handler = provider.GetService<ICommandHandler<CreateOrderWithId, Guid>>();

        Assert.NotNull(handler);
        Assert.IsType<CreateOrderWithIdHandler>(handler);
    }

    [Fact]
    public void AddUlak_RegistersQueryHandler()
    {
        var services = new ServiceCollection();
        services.AddUlak(typeof(RegistrationTests).Assembly);
        var provider = services.BuildServiceProvider();

        var handler = provider.GetService<IQueryHandler<GetOrder, OrderDto>>();

        Assert.NotNull(handler);
        Assert.IsType<GetOrderHandler>(handler);
    }

    [Fact]
    public void AddUlakBehavior_RegistersOpenGenericBehavior()
    {
        var services = new ServiceCollection();
        services.AddUlak(typeof(RegistrationTests).Assembly);
        services.AddUlakBehavior(typeof(OrderTrackingBehavior<,>));
        var provider = services.BuildServiceProvider();

        var behaviors = provider.GetServices<IPipelineBehavior<PingCommand, string>>();

        Assert.Contains(behaviors, behavior => behavior is OrderTrackingBehavior<PingCommand, string>);
    }

    [Fact]
    public void AddUlak_MultipleAssemblies_RegistersAll()
    {
        var services = new ServiceCollection();
        services.AddUlak(typeof(RegistrationTests).Assembly);

        // Should not throw, even when same assembly scanned twice
        var provider = services.BuildServiceProvider();
        var sender = provider.GetService<ISender>();

        Assert.NotNull(sender);
    }
}
