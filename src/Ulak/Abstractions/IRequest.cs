namespace Ulak;

public interface IRequest<out TResponse>
{
    internal Type RequestType => GetType();
}
