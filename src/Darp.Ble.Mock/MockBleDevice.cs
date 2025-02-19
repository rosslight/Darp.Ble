using Darp.Ble.Data;
using Darp.Ble.Implementation;
using Microsoft.Extensions.Logging;

namespace Darp.Ble.Mock;

/// <summary> Provides a mock implementation of a ble device </summary>
internal sealed class MockBleDevice(
    BleMockFactory.InitializeAsync onInitialize,
    string name,
    ILogger? logger) : BleDevice(logger)
{
    private readonly BleMockFactory.InitializeAsync _onInitialize = onInitialize;

    /// <inheritdoc />
    public override string Name { get; } = name;
    /// <inheritdoc />
    public override string Identifier => "Darp.Ble.Mock";

    /// <inheritdoc />
    protected override async Task<InitializeResult> InitializeAsyncCore(CancellationToken cancellationToken)
    {
        var broadcaster = new MockBleBroadcaster(Logger);
        var peripheral = new MockBlePeripheral(this, broadcaster, Logger);
        Observer = new MockBleObserver(this, broadcaster, Logger);
        Central = new MockBleCentral(this, peripheral, Logger);
        await _onInitialize.Invoke(broadcaster, peripheral).ConfigureAwait(false);
        return InitializeResult.Success;
    }
}