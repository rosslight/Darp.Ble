using Darp.Ble.Backends;
using Darp.Ble.Data;
using Darp.Ble.Gap;
using Darp.Ble.Hci;

namespace Darp.Ble.HciHost.Gap;

/// <summary> Creates advertising sets on the controller's advertising handles </summary>
internal sealed class HciHostBroadcasterDriver(
    HciDevice device,
    int maxDataLength,
    int numberOfSets,
    Func<BleAddress> adapterAddress
) : IBroadcasterDriver
{
    // One command carries at most 251 bytes; longer data would need fragments, which no app uses
    private const int MaxDataPerCommand = 251;

    private readonly HciDevice _device = device;
    private readonly int _maxDataLength = Math.Min(maxDataLength, MaxDataPerCommand);
    private readonly Func<BleAddress> _adapterAddress = adapterAddress;
    private readonly Lock _lock = new();
    private readonly bool[] _handlesInUse = new bool[numberOfSets];

    public async Task<IAdvertisingSetDriver> CreateAdvertisingSetAsync(
        AdvertisingParameters parameters,
        AdvertisingData data,
        AdvertisingData? scanResponseData,
        IAdvertisingSetSink sink,
        CancellationToken cancellationToken
    )
    {
        HciHostAdvertisingSetDriver.CheckType(parameters);
        HciHostAdvertisingSetDriver.CheckLength(data, _maxDataLength, nameof(data));
        if (scanResponseData is not null)
            HciHostAdvertisingSetDriver.CheckLength(scanResponseData, _maxDataLength, nameof(scanResponseData));
        byte handle = TakeHandle();
        var set = new HciHostAdvertisingSetDriver(_device, handle, _maxDataLength, () => ReleaseHandle(handle));
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

    private void ReleaseHandle(byte handle)
    {
        lock (_lock)
            _handlesInUse[handle] = false;
    }
}
