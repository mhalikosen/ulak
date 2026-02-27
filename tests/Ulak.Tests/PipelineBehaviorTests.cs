using Microsoft.Extensions.DependencyInjection;

namespace Ulak.Tests;

public class PipelineBehaviorTests
{
    [Fact]
    public async Task SingleBehavior_TransformsResponse()
    {
        var services = new ServiceCollection();
        services.AddUlak(typeof(PipelineBehaviorTests).Assembly);
        services.AddUlakBehavior(typeof(UpperCaseBehavior));
        using var provider = services.BuildServiceProvider();
        var sender = provider.GetRequiredService<ISender>();

        var result = await sender.SendAsync(new PingCommand("hello"));

        Assert.Equal("HELLO", result);
    }

    [Fact]
    public async Task MultipleBehaviors_ExecuteInRegistrationOrder()
    {
        var services = new ServiceCollection();
        services.AddUlak(typeof(PipelineBehaviorTests).Assembly);
        // First registered = outermost: Wrap runs first, then UpperCase
        services.AddScoped<IPipelineBehavior<PingCommand, string>, WrapBehavior>();
        services.AddScoped<IPipelineBehavior<PingCommand, string>, UpperCaseBehavior>();
        using var provider = services.BuildServiceProvider();
        var sender = provider.GetRequiredService<ISender>();

        // UpperCase wraps handler: "hello" -> "HELLO"
        // Wrap wraps UpperCase: "HELLO" -> "[HELLO]"
        var result = await sender.SendAsync(new PingCommand("hello"));

        Assert.Equal("[HELLO]", result);
    }

    [Fact]
    public async Task Behavior_ThrowsException_PropagatesUp()
    {
        var services = new ServiceCollection();
        services.AddUlak(typeof(PipelineBehaviorTests).Assembly);
        services.AddScoped<IPipelineBehavior<PingCommand, string>, ThrowingBehavior>();
        using var provider = services.BuildServiceProvider();
        var sender = provider.GetRequiredService<ISender>();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(()
            => sender.SendAsync(new PingCommand("hello")));

        Assert.Equal("Pipeline error", exception.Message);
    }

    [Fact]
    public async Task OpenGenericBehavior_AppliesAcrossRequestTypes()
    {
        var services = new ServiceCollection();
        services.AddUlak(typeof(PipelineBehaviorTests).Assembly);
        services.AddScoped<ExecutionTracker>();
        services.AddUlakBehavior(typeof(OrderTrackingBehavior<,>));
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        var tracker = scope.ServiceProvider.GetRequiredService<ExecutionTracker>();

        await sender.SendAsync(new PingCommand("test"));

        Assert.Equal(["Before:PingCommand", "After:PingCommand"], tracker.Log);
    }

    [Fact]
    public async Task NoBehaviors_HandlerExecutesDirectly()
    {
        var services = new ServiceCollection();
        services.AddUlak(typeof(PipelineBehaviorTests).Assembly);
        // No behaviors registered
        using var provider = services.BuildServiceProvider();
        var sender = provider.GetRequiredService<ISender>();

        var result = await sender.SendAsync(new PingCommand("direct"));

        Assert.Equal("direct", result);
    }

    [Fact]
    public async Task OpenGenericBehavior_AppliesOnVoidCommand()
    {
        var services = new ServiceCollection();
        services.AddUlak(typeof(PipelineBehaviorTests).Assembly);
        services.AddScoped<ExecutionTracker>();
        services.AddUlakBehavior(typeof(OrderTrackingBehavior<,>));
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        var tracker = scope.ServiceProvider.GetRequiredService<ExecutionTracker>();

        await sender.SendAsync(new VoidPingCommand("test"));

        Assert.Equal(["Before:VoidPingCommand", "After:VoidPingCommand"], tracker.Log);
    }
}
