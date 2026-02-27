using Microsoft.Extensions.DependencyInjection;

namespace Ulak.Tests;

public class PipelineBehaviorTests
{
    [Fact]
    public async Task SingleBehaviorTransformsResponse()
    {
        var services = new ServiceCollection();
        services.AddUlak(options =>
        {
            options.AddBehavior<UpperCaseBehavior>();
        });
        using var provider = services.BuildServiceProvider();
        var sender = provider.GetRequiredService<ISender>();

        var result = await sender.SendAsync(new PingCommand("hello"), TestContext.Current.CancellationToken);

        Assert.Equal("HELLO", result);
    }

    [Fact]
    public async Task MultipleBehaviorsExecuteInRegistrationOrder()
    {
        var services = new ServiceCollection();
        services.AddUlak(options =>
        {
            options.AddBehavior<WrapBehavior>();
            options.AddBehavior<UpperCaseBehavior>();
        });
        using var provider = services.BuildServiceProvider();
        var sender = provider.GetRequiredService<ISender>();

        var result = await sender.SendAsync(new PingCommand("hello"), TestContext.Current.CancellationToken);

        Assert.Equal("[HELLO]", result);
    }

    [Fact]
    public async Task BehaviorThrowsExceptionPropagatesUp()
    {
        var services = new ServiceCollection();
        services.AddUlak(options =>
        {
            options.AddBehavior<ThrowingBehavior>();
        });
        using var provider = services.BuildServiceProvider();
        var sender = provider.GetRequiredService<ISender>();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(()
            => sender.SendAsync(new PingCommand("hello"), TestContext.Current.CancellationToken));

        Assert.Equal("Pipeline error", exception.Message);
    }

    [Fact]
    public async Task GlobalBehaviorAppliesAcrossRequestTypes()
    {
        var services = new ServiceCollection();
        services.AddScoped<ExecutionTracker>();
        services.AddUlak(options =>
        {
            options.AddBehavior<OrderTrackingBehavior>();
        });
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        var tracker = scope.ServiceProvider.GetRequiredService<ExecutionTracker>();

        await sender.SendAsync(new PingCommand("test"), TestContext.Current.CancellationToken);

        Assert.Equal(["Before:PingCommand", "After:PingCommand"], tracker.Log);
    }

    [Fact]
    public async Task NoBehaviorsHandlerExecutesDirectly()
    {
        var services = new ServiceCollection();
        services.AddUlak();
        using var provider = services.BuildServiceProvider();
        var sender = provider.GetRequiredService<ISender>();

        var result = await sender.SendAsync(new PingCommand("direct"), TestContext.Current.CancellationToken);

        Assert.Equal("direct", result);
    }

    [Fact]
    public async Task GlobalBehaviorAppliesOnVoidCommand()
    {
        var services = new ServiceCollection();
        services.AddScoped<ExecutionTracker>();
        services.AddUlak(options =>
        {
            options.AddBehavior<OrderTrackingBehavior>();
        });
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        var tracker = scope.ServiceProvider.GetRequiredService<ExecutionTracker>();

        await sender.SendAsync(new VoidPingCommand("test"), TestContext.Current.CancellationToken);

        Assert.Equal(["Before:VoidPingCommand", "After:VoidPingCommand"], tracker.Log);
    }
}