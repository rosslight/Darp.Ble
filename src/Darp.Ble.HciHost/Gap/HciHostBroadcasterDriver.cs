using Darp.Ble.Backends;
using Darp.Ble.Data;
using Darp.Ble.Gap;
using Darp.Ble.Hci;
using Darp.Ble.Hci.Payload;
using Darp.Ble.Hci.Payload.Event;
using Darp.Utils.Messaging;

namespace Darp.Ble.HciHost.Gap;

/// <summary> Creates advertising sets on the controller's advertising handles </summary>
internal sealed partial class HciHostBroadcasterDriver : IBroadcasterDriver, IDisposable
{
    // One command carries at most 251 bytes; longer data would need fragments, which no app uses
    private const int MaxDataPerCommand = 251;

    private readonly HciDevice _device;
    private readonly int _maxDataLength;
    private readonly Func<BleAddress> _adapterAddress;
    private readonly Func<ushort, ICentralConnectionDriver?> _findLink;
    private readonly Lock _lock = new();
    private readonly bool[] _handlesInUse;
    private readonly Dictionary<byte, HciHostAdvertisingSetDriver> _sets = [];
    private readonly IDisposable _subscription;

    public HciHostBroadcasterDriver(
        HciDevice device,
        int maxDataLength,
        int numberOfSets,
        Func<BleAddress> adapterAddress,
        Func<ushort, ICentralConnectionDriver?> findLink
    )
    {
        _device = device;
        _maxDataLength = Math.Min(maxDataLength, MaxDataPerCommand);
        _handlesInUse = new bool[numberOfSets];
        _adapterAddress = adapterAddress;
        _findLink = findLink;
        _subscription = device.Host.Subscribe(this);
    }

    public async Task<IAdvertisingSetDriver> CreateAdvertisingSetAsync(
        AdvertisingParameters parameters,
        AdvertisingData data,
        AdvertisingData? scanResponseData,
        IAdvertisingSetSink sink,
        CancellationToken cancellationToken
    )
    {
        HciHostAdvertisingSetDriver.CheckLength(data, _maxDataLength, nameof(data));
        if (scanResponseData is not null)
            HciHostAdvertisingSetDriver.CheckLength(scanResponseData, _maxDataLength, nameof(scanResponseData));
        byte handle = TakeHandle();
        var set = new HciHostAdvertisingSetDriver(_device, handle, _maxDataLength, sink, () => Release(handle));
        lock (_lock)
            _sets[handle] = set;
        try
        {
            await set.CreateAsync(parameters, _adapterAddress(), cancellationToken).ConfigureAwait(false);
            if (data.Count > 0)
                await set.SetDataAsync(data, cancellationToken).ConfigureAwait(false);
            if (scanResponseData is not null)
                await set.SetScanResponseDataAsync(scanResponseData, cancellationToken).ConfigureAwait(false);
            return set;
        }
        catch
        {
            await set.DisposeAsync().ConfigureAwait(false);
            throw;
        }
    }

    private byte TakeHandle()
    {
        lock (_lock)
        {
            int handle = Array.IndexOf(_handlesInUse, false);
            if (handle < 0)
                throw new BleException($"All {_handlesInUse.Length} advertising sets of the controller are in use");
            _handlesInUse[handle] = true;
            return (byte)handle;
        }
    }

    private void Release(byte handle)
    {
        lock (_lock)
        {
            _handlesInUse[handle] = false;
            _sets.Remove(handle);
        }
    }

    [MessageSink]
    private void OnAdvertisingSetTerminated(HciLeAdvertisingSetTerminatedEvent hciEvent)
    {
        // A connection stops a connectable set; the controller reported the link before this event
        if (hciEvent.Status is not HciCommandStatus.Success)
            return;
        HciHostAdvertisingSetDriver? set;
        lock (_lock)
            set = _sets.GetValueOrDefault(hciEvent.AdvertisingHandle);
        if (set is not null && _findLink(hciEvent.ConnectionHandle) is { } link)
            set.Sink.OnStoppedByConnection(link);
    }

    public void Dispose() => _subscription.Dispose();
}
