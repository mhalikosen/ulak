using Microsoft.Extensions.DependencyInjection;

namespace Ulak.Tests;

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
    public async Task<string> HandleAsync(PingCommand request, RequestHandlerDelegate<string> next, CancellationToken cancellationToken)
    {
        var result = await next();
        return result.ToUpperInvariant();
    }
}

public class WrapBehavior : IPipelineBehavior<PingCommand, string>
{
    public async Task<string> HandleAsync(PingCommand request, RequestHandlerDelegate<string> next, CancellationToken cancellationToken)
    {
        var result = await next();
        return $"[{result}]";
    }
}

public class ThrowingBehavior : IPipelineBehavior<PingCommand, string>
{
    public Task<string> HandleAsync(PingCommand request, RequestHandlerDelegate<string> next, CancellationToken cancellationToken)
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
    public async Task<TResponse> HandleAsync(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        tracker.Log.Add($"Before:{typeof(TRequest).Name}");
        var result = await next();
        tracker.Log.Add($"After:{typeof(TRequest).Name}");
        return result;
    }
}

public class PipelineBehaviorTests
{
    [Fact]
    public async Task SingleBehavior_TransformsResponse()
    {
        var services = new ServiceCollection();
        services.AddUlak(typeof(PipelineBehaviorTests).Assembly);
        services.AddUlakBehavior(typeof(UpperCaseBehavior));
        var sender = services.BuildServiceProvider().GetRequiredService<ISender>();

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
        var sender = services.BuildServiceProvider().GetRequiredService<ISender>();

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
        var sender = services.BuildServiceProvider().GetRequiredService<ISender>();

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
        var sender = services.BuildServiceProvider().GetRequiredService<ISender>();

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
