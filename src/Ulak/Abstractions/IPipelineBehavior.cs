namespace Ulak;

public delegate Task<TResponse> PipelineStep<TResponse>();

public interface IPipelineBehavior<in TRequest, TResponse> where TRequest : IRequest<TResponse>
{
    public Task<TResponse> HandleAsync(TRequest request, PipelineStep<TResponse> nextHandler, CancellationToken cancellationToken);
}