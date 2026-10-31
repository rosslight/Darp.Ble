using Darp.Ble.Backends;
using Darp.Ble.Data;
using Darp.Ble.Gap;
using Darp.Ble.Hci;
using Darp.Ble.Hci.Host;
using Darp.Ble.Hci.Payload.Command;
using Darp.Ble.Hci.Payload.Event;
using Darp.Ble.Hci.Payload.Result;
using Darp.Utils.Messaging;

namespace Darp.Ble.HciHost.Gap;

/// <summary> Runs the controller's extended scan and reports what it hears on the transport's read loop </summary>
internal sealed partial class HciHostObserverDriver : IObserverDriver, IDisposable
{
    private readonly HciDevice _device;
    private readonly IDisposable _subscription;
    private readonly Lock _lock = new();
    private IScanSink? _sink;
    private bool _transportFailed;

    // The type each address was last heard with, so an address known only by its value can be connected.
    // Bounded: a long scan among devices with changing private addresses hears ever new ones
    private const int MaxKnownAddresses = 1024;
    private readonly Dictionary<ulong, BleAddressType> _addressTypes = [];

    // Used on the read loop only; a new scan's sink starts it afresh
    private readonly AdvertisingReportAssembler _assembler = new();
    private IScanSink? _assembledFor;

    public HciHostObserverDriver(HciDevice device)
    {
        _device = device;
        _subscription = device.Host.Subscribe(this);
        device.Host.TransportFailed += OnTransportFailed;
    }

    public async Task StartScanAsync(BleScanParameters parameters, IScanSink sink, CancellationToken cancellationToken)
    {
        var scanParameters = new HciLeSetExtendedScanParametersCommand
        {
            OwnAddressType = 0x01,
            ScanningFilterPolicy = 0x00,
            ScanPhys = 0b00000001,
            ScanType = parameters.ScanType is ScanType.Active ? (byte)0x01 : (byte)0x00,
            ScanInterval = HciTiming.ToScanUnits(parameters.ScanInterval, nameof(parameters.ScanInterval)),
            ScanWindow = HciTiming.ToScanUnits(parameters.ScanWindow, nameof(parameters.ScanWindow)),
        };
        // The first reports may arrive before the enable command completes
        lock (_lock)
            _sink = sink;
        try
        {
            await _device
                .RunAsync<HciLeSetExtendedScanParametersCommand, HciSetExtendedScanParametersResult>(
                    scanParameters,
                    "set the scan parameters",
                    cancellationToken
                )
                .ConfigureAwait(false);
            await _device
                .RunAsync<HciLeSetExtendedScanEnableCommand, HciSetExtendedScanEnableResult>(
                    Enable(enabled: true),
                    "start scanning",
                    cancellationToken
                )
                .ConfigureAwait(false);
        }
        catch
        {
            lock (_lock)
                _sink = null;
            throw;
        }
    }

    public async Task StopScanAsync(CancellationToken cancellationToken)
    {
        bool transportFailed;
        lock (_lock)
        {
            _sink = null;
            transportFailed = _transportFailed;
        }
        // Without a transport the controller does not scan either
        if (transportFailed)
            return;
        // The controller answers Command Disallowed when it did not scan anymore, which is fine
        await _device
            .Host.QueryCommandCompletionAsync<HciLeSetExtendedScanEnableCommand, HciSetExtendedScanEnableResult>(
                Enable(enabled: false),
                cancellationToken
            )
            .ConfigureAwait(false);
    }

    /// <summary> The type the address was last heard with, if it was heard </summary>
    /// <param name="address"> The value of the address </param>
    /// <returns> The address type, or null </returns>
    public BleAddressType? TypeOf(Data.UInt48 address)
    {
        lock (_lock)
            return _addressTypes.TryGetValue((ulong)address, out BleAddressType type) ? type : null;
    }

    private void Remember(ulong address, BleAddressType type)
    {
        // Anonymous advertisements have no address to connect to
        if (type is BleAddressType.NotAvailable)
            return;
        if (_addressTypes.Count >= MaxKnownAddresses && !_addressTypes.ContainsKey(address))
            _addressTypes.Clear();
        _addressTypes[address] = type;
    }

    [MessageSink]
    private void OnAdvertisingReport(HciLeExtendedAdvertisingReportEvent reportEvent)
    {
        IScanSink? sink;
        lock (_lock)
        {
            sink = _sink;
            foreach (HciLeExtendedAdvertisingReport report in reportEvent.Reports)
                Remember(report.Address.ToUInt64(), (BleAddressType)report.AddressType);
        }
        if (sink is null)
            return;
        if (!ReferenceEquals(sink, _assembledFor))
        {
            _assembler.Clear();
            _assembledFor = sink;
        }
        DateTimeOffset timestamp = _device.TimeProvider.GetUtcNow();
        foreach (HciLeExtendedAdvertisingReport report in reportEvent.Reports)
        {
            if (!_assembler.TryEnd(report, out ReadOnlyMemory<byte> data, out sbyte txPower))
                continue;
            sink.OnReport(
                GapAdvertisement.Create(
                    timestamp,
                    // Without the data status in bits 5 and 6: the advertisement is whole
                    (BleEventType)(report.EventType & 0b11111),
                    new BleAddress((BleAddressType)report.AddressType, (Data.UInt48)report.Address.ToUInt64()),
                    (Physical)report.PrimaryPhy,
                    (Physical)report.SecondaryPhy,
                    (AdvertisingSId)report.AdvertisingSId,
                    (TxPowerLevel)txPower,
                    (Rssi)report.Rssi,
                    (PeriodicAdvertisingInterval)report.PeriodicAdvertisingInterval,
                    new BleAddress(
                        (BleAddressType)report.DirectAddressType,
                        (Data.UInt48)report.DirectAddress.ToUInt64()
                    ),
                    AdvertisingData.From(data)
                )
            );
        }
    }

    private void OnTransportFailed(object? sender, HciTransportFailedEventArgs args)
    {
        IScanSink? sink;
        lock (_lock)
        {
            _transportFailed = true;
            sink = _sink;
            _sink = null;
        }
        sink?.OnFailed(new BleException("The HCI transport failed while scanning", args.Exception));
    }

    private static HciLeSetExtendedScanEnableCommand Enable(bool enabled) =>
        new()
        {
            Enable = enabled ? (byte)0x01 : (byte)0x00,
            FilterDuplicates = 0x00,
            Duration = 0x0000,
            Period = 0x0000,
        };

    public void Dispose()
    {
        _device.Host.TransportFailed -= OnTransportFailed;
        _subscription.Dispose();
    }
}
