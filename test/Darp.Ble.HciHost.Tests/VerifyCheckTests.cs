using VerifyTUnit;

namespace Darp.Ble.HciHost.Tests;

public sealed class VerifyCheckTests
{
    [Test]
    public Task Run()
    {
        return VerifyChecks.Run();
    }
}
