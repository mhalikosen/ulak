# Ulak

Lightweight CQRS mediator library for .NET.

## Installation

```bash
dotnet add package Ulak
```

## Quick Start

### 1. Define commands and queries

```csharp
public record CreateUser(string Name, string Email) : ICommand;

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

app.MapGet("/users/{id}", async (Guid id, ISender sender) =>
{
    var user = await sender.SendAsync(new GetUser(id));
    return Results.Ok(user);
});
```

### 5. Pipeline behaviors (optional)

```csharp
public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> HandleAsync(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        Console.WriteLine($"Handling {typeof(TRequest).Name}");
        var response = await next();
        Console.WriteLine($"Handled {typeof(TRequest).Name}");
        return response;
    }
}

builder.Services.AddUlakBehavior(typeof(LoggingBehavior<,>));
```

## License

MIT
