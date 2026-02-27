namespace Ulak;

public readonly struct Unit : IEquatable<Unit>
{
    public static readonly Unit Value;
    public static readonly Task<Unit> Task = System.Threading.Tasks.Task.FromResult(Value);

    public bool Equals(Unit other) => true;
    public override bool Equals(object? obj) => obj is Unit;
    public override int GetHashCode() => 0;
    public override string ToString() => "()";

#pragma warning disable IDE0060 // Remove unused parameter - required by operator signature
    public static bool operator ==(Unit left, Unit right) => true;
    public static bool operator !=(Unit left, Unit right) => false;
#pragma warning restore IDE0060
}
