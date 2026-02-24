namespace Ulak;

public interface IQuery<out TResponse> : IRequest<TResponse>;
