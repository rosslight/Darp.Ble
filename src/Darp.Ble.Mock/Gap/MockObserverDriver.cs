using Darp.Ble.Backends;
using Darp.Ble.Data;
using Darp.Ble.Gap;

namespace Darp.Ble.Mock.Gap;

internal sealed class MockObserverDriver(MockAir air, MockAdapterDriver adapter) : IObserverDriver
{
    private readonly MockAir _air = air;
    private readonly Lock _lock = new();
    private (IScanSink Sink, BleScanParameters Parameters)? _scan;

    public MockAdapterDriver Adapter { get; } = adapter;

    public Task StartScanAsync(BleScanParameters parameters, IScanSink sink, CancellationToken cancellationToken)
    {
        lock (_lock)
            _scan = (sink, parameters);
        _air.StartListening(this);
        return Task.CompletedTask;
    }

    public Task StopScanAsync(CancellationToken cancellationToken)
    {
        _air.StopListening(this);
        lock (_lock)
            _scan = null;
        return Task.CompletedTask;
    }

    /// <summary> Receives one advertising event; an active scan also gets the scan response of a scannable set </summary>
    public void Receive(MockAdvertisingState advertising, DateTimeOffset timestamp, Rssi rssi)
    {
        (IScanSink Sink, BleScanParameters Parameters)? scan;
        lock (_lock)
            scan = _scan;
        if (scan is not { } running)
            return;
        AdvertisingParameters parameters = advertising.Parameters;
        running.Sink.OnReport(CreateReport(advertising, parameters.Type, advertising.Data, timestamp, rssi));
        if (
            running.Parameters.ScanType is ScanType.Active
            && parameters.Type.HasFlag(BleEventType.Scannable)
            && advertising.ScanResponseData is { } scanResponse
        )
        {
            running.Sink.OnReport(
                CreateReport(advertising, parameters.Type | BleEventType.ScanResponse, scanResponse, timestamp, rssi)
            );
        }
    }

    private static GapAdvertisement CreateReport(
        MockAdvertisingState advertising,
        BleEventType eventType,
        AdvertisingData data,
        DateTimeOffset timestamp,
        Rssi rssi
    )
    {
        AdvertisingParameters parameters = advertising.Parameters;
        bool isLegacy = eventType.HasFlag(BleEventType.Legacy);
        return GapAdvertisement.Create(
            timestamp,
            eventType,
            advertising.Address,
            parameters.PrimaryPhy,
            isLegacy ? Physical.NotAvailable : parameters.SecondaryPhy,
            isLegacy ? AdvertisingSId.NoAdIProvided : parameters.AdvertisingSId,
            parameters.AdvertisingTxPower,
            rssi,
            PeriodicAdvertisingInterval.NoPeriodicAdvertising,
            eventType.HasFlag(BleEventType.Directed)
                ? parameters.PeerAddress ?? BleAddress.NotAvailable
                : BleAddress.NotAvailable,
            data
        );
    }
}
