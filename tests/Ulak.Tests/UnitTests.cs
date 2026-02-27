namespace Ulak.Tests;

public class UnitTests
{
    [Fact]
    public void EqualsUnitReturnsTrue()
    {
        var a = Unit.Value;
        var b = Unit.Value;

        Assert.True(a.Equals(b));
    }

    [Fact]
    public void EqualsObjectUnitReturnsTrue()
    {
        var a = Unit.Value;
        object b = Unit.Value;

        Assert.True(a.Equals(b));
    }

    [Fact]
    public void EqualsNonUnitObjectReturnsFalse()
    {
        var a = Unit.Value;

        Assert.False(a.Equals("not a unit"));
        Assert.False(a.Equals(42));
        Assert.False(a.Equals(null));
    }

    [Fact]
    public void EqualityOperatorReturnsTrue()
    {
        var a = Unit.Value;
        var b = Unit.Value;

        Assert.True(a == b);
    }

    [Fact]
    public void InequalityOperatorReturnsFalse()
    {
        var a = Unit.Value;
        var b = Unit.Value;

        Assert.False(a != b);
    }

    [Fact]
    public void GetHashCodeReturnsZero()
    {
        Assert.Equal(0, Unit.Value.GetHashCode());
    }

    [Fact]
    public void ToStringReturnsParentheses()
    {
        Assert.Equal("()", Unit.Value.ToString());
    }

    [Fact]
    public async Task TaskReturnsCompletedUnitTask()
    {
        var result = await Unit.Task;

        Assert.Equal(Unit.Value, result);
    }
}