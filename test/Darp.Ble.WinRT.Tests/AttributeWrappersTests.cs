using Darp.Ble.WinRT.Connections;
using Shouldly;

namespace Darp.Ble.WinRT.Tests;

/// <summary> Every native GATT object of a connection has one owner, also when a discovery completes after the end </summary>
public sealed class AttributeWrappersTests
{
    private readonly List<Native> _released = [];
    private readonly AttributeWrappers<Native, Wrapper> _wrappers;

    public AttributeWrappersTests() =>
        _wrappers = new AttributeWrappers<Native, Wrapper>(n => n.Handle, n => new Wrapper(n), _released.Add);

    [Fact]
    public void ARediscoveredAttribute_GetsItsFirstWrapper_AndTheNewNativeObjectIsReleased()
    {
        var first = new Native(1);
        var again = new Native(1);
        Wrapper wrapper = _wrappers.Wrap([first]).ShouldNotBeNull().ShouldHaveSingleItem();

        _wrappers.Wrap([again]).ShouldNotBeNull().ShouldHaveSingleItem().ShouldBeSameAs(wrapper);

        _released.ShouldBe([again]);
    }

    [Fact]
    public void ADiscoveryThatCompletesAfterTheEnd_WrapsNothing_AndReleasesItsNativeObjects()
    {
        Wrapper early = _wrappers.Wrap([new Native(1)]).ShouldNotBeNull().ShouldHaveSingleItem();

        _wrappers.End().ShouldBe([early]);
        var late = new Native(2);
        var known = new Native(1);

        _wrappers.Wrap([late, known]).ShouldBeNull();
        _released.ShouldBe([late, known]);
    }

    // Classes, not records: the tests tell apart two native objects of the same handle
    private sealed class Native(ushort handle)
    {
        public ushort Handle { get; } = handle;
    }

    private sealed class Wrapper(Native native)
    {
        public Native Native { get; } = native;
    }
}
