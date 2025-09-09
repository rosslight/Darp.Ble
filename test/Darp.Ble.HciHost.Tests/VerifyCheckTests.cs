namespace Darp.Ble.HciHost.Tests;

public sealed class VerifyCheckTests
{
    [Fact]
    public Task Run()
    {
        return VerifyChecks.Run();
    }
}
