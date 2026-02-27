# Ulak

[![NuGet](https://img.shields.io/nuget/v/Ulak.svg)](https://www.nuget.org/packages/Ulak)
[![NuGet Downloads](https://img.shields.io/nuget/dt/Ulak.svg)](https://www.nuget.org/packages/Ulak)

Lightweight CQRS mediator library for .NET.

## Installation

```bash
dotnet add package Ulak
```

Requires .NET 10+.

## Quick Start

### 1. Define commands and queries

```csharp
public record CreateUser(string Name, string Email) : ICommand;

public record CreateOrder(Guid UserId, decimal Amount) : ICommand<Guid>;

public record GetUser(Guid Id) : IQuery<UserDto>;
```

### 2. Implement handlers

```csharp
public class CreateUserHandler : ICommandHandler<CreateUser>
{
    public async Task HandleAsync(CreateUser command, CancellationToken cancellationToken)
    {
        // create user...
    }
}

public class CreateOrderHandler : ICommandHandler<CreateOrder, Guid>
{
    public async Task<Guid> HandleAsync(CreateOrder command, CancellationToken cancellationToken)
    {
        // create order, return id...
    }
}

public class GetUserHandler : IQueryHandler<GetUser, UserDto>
{
    public async Task<UserDto> HandleAsync(GetUser query, CancellationToken cancellationToken)
    {
        // fetch user...
    }
}
```

### 3. Register services

```csharp
builder.Services.AddUlak(typeof(Program).Assembly);
```

### 4. Send requests

```csharp
app.MapPost("/users", async (CreateUser command, ISender sender) =>
{
    await sender.SendAsync(command);
    return Results.Created();
});

app.MapPost("/orders", async (CreateOrder command, ISender sender) =>
{
    var orderId = await sender.SendAsync(command);
    return Results.Created($"/orders/{orderId}", new { id = orderId });
});

app.MapGet("/users/{id}", async (Guid id, ISender sender) =>
{
    var user = await sender.SendAsync(new GetUser(id));
    return Results.Ok(user);
});
```

### 5. Pipeline behaviors (optional)

Behaviors run in registration order, wrapping the handler in a pipeline.

```csharp
public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> HandleAsync(
        TRequest request,
        NextStep<TResponse> next,
        CancellationToken cancellationToken)
    {
        Console.WriteLine($"Handling {typeof(TRequest).Name}");
        var response = await next();
        Console.WriteLine($"Handled {typeof(TRequest).Name}");
        return response;
    }
}

// Open generic — applies to all requests
builder.Services.AddUlakBehavior(typeof(LoggingBehavior<,>));

// Concrete — applies to a specific request type
builder.Services.AddUlakBehavior(typeof(UpperCaseBehavior));
```

## License

MIT
