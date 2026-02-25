namespace Ulak;

public interface ISender
{
    Task<TResponse> SendAsync<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default);

    Task SendAsync(ICommand command, CancellationToken cancellationToken = default);
}
