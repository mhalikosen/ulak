namespace Ulak;

#pragma warning disable CA1711 // Identifiers should not have incorrect suffix
public delegate Task<TResponse> RequestHandlerDelegate<TResponse>();
#pragma warning restore CA1711

public interface IPipelineBehavior<in TRequest, TResponse> where TRequest : IRequest<TResponse>
{
    public Task<TResponse> HandleAsync(TRequest request, RequestHandlerDelegate<TResponse> nextHandler, CancellationToken cancellationToken);
}
