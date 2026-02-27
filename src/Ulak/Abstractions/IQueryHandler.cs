namespace Ulak;

public interface IQueryHandler<in TQuery, TResponse> where TQuery : IQuery<TResponse>
{
    public Task<TResponse> HandleAsync(TQuery query, CancellationToken cancellationToken);
}