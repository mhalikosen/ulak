namespace Ulak;

public interface ISender
{
    public Task<TResponse> SendAsync<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default);

    public Task SendAsync(ICommand command, CancellationToken cancellationToken = default);
}
