namespace Ulak;

public delegate Task<TResponse> NextStep<TResponse>();

public interface IPipelineBehavior<in TRequest, TResponse> where TRequest : IRequest<TResponse>
{
    public Task<TResponse> HandleAsync(TRequest request, NextStep<TResponse> nextHandler, CancellationToken cancellationToken);
}