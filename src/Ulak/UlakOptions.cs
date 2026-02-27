namespace Ulak;

public class UlakOptions
{
    internal List<Type> BehaviorTypes { get; } = [];

    public UlakOptions AddBehavior<T>() where T : IPipelineBehavior
    {
        BehaviorTypes.Add(typeof(T));
        return this;
    }
}