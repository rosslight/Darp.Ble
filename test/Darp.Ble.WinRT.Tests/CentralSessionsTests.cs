using Darp.Ble.WinRT.Connections;
using Shouldly;

namespace Darp.Ble.WinRT.Tests;

/// <summary> One central per device for the connection's lifetime, across Windows' sessions </summary>
public sealed class CentralSessionsTests
{
    private readonly CentralSessions<object, string> _sessions = new();
    private int _created;

    private string Create() => $"central {++_created}";

    [Fact]
    public void ADevicesSessions_ShareOneCentral_AndEachIsNewOnce()
    {
        object first = new();
        object second = new();

        _sessions.Resolve("device", first, Create).ShouldBe(("central 1", true, true));
        _sessions.Resolve("device", second, Create).ShouldBe(("central 1", false, true));
        _sessions.Resolve("device", second, Create).ShouldBe(("central 1", false, false));
    }

    [Fact]
    public void TheCentralEnds_OnceItsLastSessionClosed_AndTheNextActivityGetsANewOne()
    {
        object first = new();
        object second = new();
        _sessions.Resolve("device", first, Create);
        _sessions.Resolve("device", second, Create);

        _sessions.Close(first).ShouldBeNull();
        _sessions.Close(second).ShouldBe("central 1");

        _sessions.Resolve("device", new object(), Create).ShouldBe(("central 2", true, true));
    }

    [Fact]
    public void ACallbackOfAClosedSession_IsIgnored()
    {
        object session = new();
        _sessions.Resolve("device", session, Create);
        _sessions.Close(session);

        _sessions.Resolve("device", session, Create).ShouldBe((null, false, false));
    }

    [Fact]
    public void EndingACentral_IgnoresTheLateCallbacksOfItsSessions()
    {
        object session = new();
        string central = _sessions.Resolve("device", session, Create).Central.ShouldNotBeNull();

        _sessions.End(central).ShouldBeTrue();

        _sessions.Resolve("device", session, Create).ShouldBe((null, false, false));
        _sessions.Close(session).ShouldBeNull();
        _sessions.Resolve("device", new object(), Create).ShouldBe(("central 2", true, true));
    }

    [Fact]
    public void Devices_AreTrackedApart()
    {
        _sessions.Resolve("device", new object(), Create);

        _sessions.Resolve("other", new object(), Create).ShouldBe(("central 2", true, true));
    }
}
