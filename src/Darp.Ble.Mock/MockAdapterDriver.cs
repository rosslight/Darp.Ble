using Darp.Ble.Backends;
using Darp.Ble.Connections;
using Darp.Ble.Data;
using Darp.Ble.Mock.Connections;
using Darp.Ble.Mock.Gap;
using Microsoft.Extensions.Logging;

namespace Darp.Ble.Mock;

/// <summary> A mock adapter or simulated peripheral: scans and advertises in its air </summary>
internal sealed class MockAdapterDriver : IBleAdapterDriver
{
    private readonly List<BleAdapter> _peripherals = [];
    private readonly CancellationTokenSource _closing = new();

    // Runs once the adapter is disposed, so its source can be opened again
    private readonly Action _closed;

    // Null for the mock adapter, which is always in range
    private readonly MockRange? _range;
    private BleAddress _randomAddress = BleAddress.NewRandomStaticAddress();

    public MockAdapterDriver(
        MockAir air,
        bool includeGattService,
        MockRange? range,
        Action closed,
        ILoggerFactory loggerFactory
    )
    {
        _closed = closed;
        _range = range;
        Background = new MockBackgroundWork(
            air.TimeProvider,
            loggerFactory.CreateLogger<MockBackgroundWork>(),
            _closing.Token
        );
        PeripheralDriver = new MockPeripheralDriver(includeGattService);
        Observer = new MockObserverDriver(air, this);
        Broadcaster = new MockBroadcasterDriver(air, this);
        CentralDriver = new MockCentralDriver(air, this);
        // Last, once the driver can lose its links: it stays in the range until it is disposed
        _range?.Join(this);
    }

    public Capabilities Features => Capabilities.SetRandomAddress | Capabilities.SetGapIdentity;

    public IObserverDriver? Observer { get; }

    public IBroadcasterDriver? Broadcaster { get; }

    public ICentralDriver? Central => CentralDriver;

    public IPeripheralDriver? Peripheral => PeripheralDriver;

    /// <summary> The peripheral role, for centrals in the air that connect to this adapter </summary>
    public MockPeripheralDriver PeripheralDriver { get; }

    /// <summary> The central role, for connections from this adapter to peripherals in the air </summary>
    public MockCentralDriver CentralDriver { get; }

    /// <summary> Whether the other adapters in the air hear and reach this one </summary>
    public bool IsInRange => _range?.IsInRange ?? true;

    public BleAddress RandomAddress => _randomAddress;

    public Task SetRandomAddressAsync(BleAddress address, CancellationToken cancellationToken)
    {
        _randomAddress = address;
        return Task.CompletedTask;
    }

    /// <summary> The work the simulated peripherals keep running while this adapter is open </summary>
    public MockBackgroundWork Background { get; }

    public void AddPeripheral(BleAdapter peripheral) => _peripherals.Add(peripheral);

    /// <summary> Ends every link of this adapter, as a central and as a peripheral, without either side ending it </summary>
    public void LoseLinks(DisconnectReason reason)
    {
        foreach (MockLink link in PeripheralDriver.Links.Concat(CentralDriver.Links))
            link.Lose(reason);
    }

    /// <summary> Stops the background work, then disposes the simulated peripherals; tries all of them even if one fails </summary>
    public async ValueTask DisposeAsync()
    {
        _range?.Quit(this);
        PeripheralDriver.Close();
        List<Exception> failures = [];
        // The work may still use the peripherals, so it ends first, also when one of its cancellation callbacks throws
        try
        {
            await _closing.CancelAsync().ConfigureAwait(false);
        }
        catch (Exception e)
        {
            failures.AddRange(e is AggregateException callbacks ? callbacks.InnerExceptions : [e]);
        }
        failures.AddRange(await Background.WhenStoppedAsync().ConfigureAwait(false));
        _closing.Dispose();
        foreach (BleAdapter peripheral in _peripherals)
        {
            try
            {
                await peripheral.DisposeAsync().ConfigureAwait(false);
            }
            catch (Exception e)
            {
                failures.Add(e);
            }
        }
        _closed();
        if (failures.Count > 0)
            throw new AggregateException("Not every mock peripheral ended cleanly", failures);
    }
}
