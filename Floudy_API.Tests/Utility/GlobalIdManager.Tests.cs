using Floudy.API.Tests.Helpers;
using Floudy.API.Utility;
using Xunit;

namespace Floudy.API.Tests.Utility;

[Collection("Sequential")]
public class GlobalIdManagerTests : IDisposable
{
    public GlobalIdManagerTests() => GlobalIdManagerHelper.Reset();
    public void Dispose() => GlobalIdManagerHelper.Reset();

    [Fact]
    public void NextAvailable_InitialState_ReturnsOne() => Assert.Equal(1, GlobalIdManager.NextAvailable());

    [Fact]
    public void NextAvailable_AfterUnregister_ReturnsRecycledId()
    {
        var id = GlobalIdManager.Register();

        GlobalIdManager.Register();
        GlobalIdManager.Unregister(id);

        Assert.Equal(1, GlobalIdManager.NextAvailable());
    }

    [Fact]
    public void Register_SequentialCalls_ReturnIncrementing()
    {
        var first = GlobalIdManager.Register();
        var second = GlobalIdManager.Register();
        var third = GlobalIdManager.Register();

        Assert.Equal(1, first);
        Assert.Equal(2, second);
        Assert.Equal(3, third);
    }

    [Fact]
    public void Register_AfterUnregister_RecyclesMostRecentlyReturnedId()
    {
        var a = GlobalIdManager.Register();
        var b = GlobalIdManager.Register();
        GlobalIdManager.Unregister(b);

        var recycled = GlobalIdManager.Register();

        Assert.Equal(b, recycled);
        _ = a;
    }

    [Fact]
    public void Register_MultipleRecycles_RespectsLifoOrder()
    {
        var a = GlobalIdManager.Register();
        var b = GlobalIdManager.Register();
        _ = GlobalIdManager.Register();
        GlobalIdManager.Unregister(a);
        GlobalIdManager.Unregister(b);

        var first_recycled = GlobalIdManager.Register();
        var second_recycled = GlobalIdManager.Register();

        Assert.Equal(b, first_recycled);
        Assert.Equal(a, second_recycled);
    }

    [Fact]
    public void Unregister_ValidRegisteredId_ReturnsTrue()
    {
        var id = GlobalIdManager.Register();
        Assert.True(GlobalIdManager.Unregister(id));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(99)]
    public void Unregister_InvalidValue_ReturnsFalse(long value) => Assert.False(GlobalIdManager.Unregister(value));

    [Fact]
    public void Unregister_IdAlreadyInAvailablePool_ReturnsFalse() => Assert.False(GlobalIdManager.Unregister(1));

    [Fact]
    public void Unregister_AlreadyUnregisteredId_ReturnsFalse()
    {
        var id = GlobalIdManager.Register();
        GlobalIdManager.Unregister(id);

        Assert.False(GlobalIdManager.Unregister(id));
    }
}