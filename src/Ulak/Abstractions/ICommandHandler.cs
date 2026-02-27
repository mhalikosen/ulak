namespace Ulak;

public interface ICommandHandler<in TCommand> where TCommand : ICommand
{
    public Task HandleAsync(TCommand command, CancellationToken cancellationToken);
}

public interface ICommandHandler<in TCommand, TResponse> where TCommand : ICommand<TResponse>
{
    public Task<TResponse> HandleAsync(TCommand command, CancellationToken cancellationToken);
}
