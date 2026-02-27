using Microsoft.Extensions.DependencyInjection;

namespace Ulak.Tests;

public class RegistrationTests
{
    [Fact]
    public void AddUlak_RegistersISender()
    {
        var services = new ServiceCollection();
        services.AddUlak(typeof(RegistrationTests).Assembly);
        using var provider = services.BuildServiceProvider();

        var sender = provider.GetService<ISender>();

        Assert.NotNull(sender);
    }

    [Fact]
    public void AddUlak_RegistersVoidCommandHandler()
    {
        var services = new ServiceCollection();
        services.AddUlak(typeof(RegistrationTests).Assembly);
        using var provider = services.BuildServiceProvider();

        var handler = provider.GetService<ICommandHandler<CreateOrder>>();

        Assert.NotNull(handler);
        Assert.IsType<CreateOrderHandler>(handler);
    }

    [Fact]
    public void AddUlak_RegistersCommandHandlerWithResponse()
    {
        var services = new ServiceCollection();
        services.AddUlak(typeof(RegistrationTests).Assembly);
        using var provider = services.BuildServiceProvider();

        var handler = provider.GetService<ICommandHandler<CreateOrderWithId, Guid>>();

        Assert.NotNull(handler);
        Assert.IsType<CreateOrderWithIdHandler>(handler);
    }

    [Fact]
    public void AddUlak_RegistersQueryHandler()
    {
        var services = new ServiceCollection();
        services.AddUlak(typeof(RegistrationTests).Assembly);
        using var provider = services.BuildServiceProvider();

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
        services.AddScoped<ExecutionTracker>();
        using var provider = services.BuildServiceProvider();

        var behaviors = provider.GetServices<IPipelineBehavior<PingCommand, string>>();

        Assert.Contains(behaviors, behavior => behavior is OrderTrackingBehavior<PingCommand, string>);
    }

    [Fact]
    public void AddUlak_SingleAssembly_RegistersSuccessfully()
    {
        var services = new ServiceCollection();
        services.AddUlak(typeof(RegistrationTests).Assembly);

        // Should not throw, even when same assembly scanned twice
        using var provider = services.BuildServiceProvider();
        var sender = provider.GetService<ISender>();

        Assert.NotNull(sender);
    }

    [Fact]
    public void AddUlak_DuplicateRegistration_FirstHandlerWins()
    {
        var services = new ServiceCollection();
        // Register same assembly twice — TryAddScoped should prevent duplicates
        services.AddUlak(typeof(RegistrationTests).Assembly, typeof(RegistrationTests).Assembly);
        using var provider = services.BuildServiceProvider();

        var handlers = provider.GetServices<ICommandHandler<CreateOrder>>();

        Assert.Single(handlers);
    }

    [Fact]
    public void AddUlak_NullAssemblies_ThrowsArgumentNullException()
    {
        var services = new ServiceCollection();

        Assert.Throws<ArgumentNullException>(()
            => services.AddUlak(null!));
    }

    [Fact]
    public void AddUlak_EmptyAssemblies_ThrowsArgumentOutOfRangeException()
    {
        var services = new ServiceCollection();

        Assert.Throws<ArgumentOutOfRangeException>(()
            => services.AddUlak());
    }

    [Fact]
    public void AddUlakBehavior_NullType_ThrowsArgumentNullException()
    {
        var services = new ServiceCollection();

        Assert.Throws<ArgumentNullException>(()
            => services.AddUlakBehavior(null!));
    }

    [Fact]
    public void AddUlakBehavior_InvalidType_ThrowsArgumentException()
    {
        var services = new ServiceCollection();

        Assert.Throws<ArgumentException>(()
            => services.AddUlakBehavior(typeof(string)));
    }

    [Fact]
    public void AddUlakBehavior_InvalidOpenGenericType_ThrowsArgumentException()
    {
        var services = new ServiceCollection();

        Assert.Throws<ArgumentException>(()
            => services.AddUlakBehavior(typeof(List<>)));
    }
}
