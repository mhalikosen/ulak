using Microsoft.Extensions.DependencyInjection;

namespace Ulak.Tests;

public record PingCommand(string Message) : ICommand<string>;

public class PingHandler : ICommandHandler<PingCommand, string>
{
    public Task<string> HandleAsync(PingCommand command, CancellationToken cancellationToken)
    {
        return Task.FromResult(command.Message);
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

public class OrderTrackingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public static List<string> ExecutionLog { get; } = [];

    public async Task<TResponse> HandleAsync(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        ExecutionLog.Add($"Before:{typeof(TRequest).Name}");
        var result = await next();
        ExecutionLog.Add($"After:{typeof(TRequest).Name}");
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
        OrderTrackingBehavior<PingCommand, string>.ExecutionLog.Clear();

        var services = new ServiceCollection();
        services.AddUlak(typeof(PipelineBehaviorTests).Assembly);
        services.AddUlakBehavior(typeof(OrderTrackingBehavior<,>));
        var sender = services.BuildServiceProvider().GetRequiredService<ISender>();

        await sender.SendAsync(new PingCommand("test"));

        Assert.Equal(["Before:PingCommand", "After:PingCommand"], OrderTrackingBehavior<PingCommand, string>.ExecutionLog);
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
}
