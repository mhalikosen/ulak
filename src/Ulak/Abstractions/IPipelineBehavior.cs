namespace Ulak;

public delegate Task<TResponse> NextStep<TResponse>();

public interface IPipelineBehavior
{
    Task<TResponse> HandleAsync<TRequest, TResponse>(
        TRequest request,
        NextStep<TResponse> nextHandler,
        CancellationToken cancellationToken)
        where TRequest : IRequest<TResponse>;
}