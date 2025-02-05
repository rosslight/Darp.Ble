using System.Collections;
using Darp.Ble.Data;
using Darp.Ble.Implementation;
using Microsoft.Extensions.Logging;

namespace Darp.Ble.Gatt.Client;

/// <summary> An interface defining a gatt attribute with a start handle and an end handle </summary>
public interface IGattAttribute
{
    /// <summary> The start handle of the attribute </summary>
    ushort StartHandle { get; }

    /// <summary> The end handle of the attribute </summary>
    ushort EndHandle { get; }
}

public interface IReadOnlyAttCollection : IReadOnlyCollection<IGattAttribute>
{
    ushort StartHandle { get; }
    ushort EndHandle { get; }
}

public sealed class AttCollection(IReadOnlyAttCollection? parentCollection) : IReadOnlyAttCollection
{
    private readonly IReadOnlyAttCollection? _parentCollection = parentCollection;
    private readonly List<IGattAttribute> _attributes = [];

    /// <summary> Get the handle of the attribute </summary>
    /// <param name="attribute"></param>
    /// <returns></returns>
    public int GetStartHandle(IGattAttribute attribute)
    {
        ushort currentHandle = StartHandle;
        for (var i = 0; i < _attributes.Count; i++)
        {
            if (_attributes[i] == attribute)
            {
                return currentHandle;
            }
            currentHandle = _attributes[i].EndHandle;
        }
        return -1;
    }

    public ushort StartHandle => _parentCollection?.EndHandle ?? 0;
    public ushort EndHandle { get; }

    public IEnumerator<IGattAttribute> GetEnumerator() => _attributes.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public int Count => _attributes.Count;
}

/// <summary> A gatt client service </summary>
/// <param name="uuid"> The UUID of the client service </param>
/// <param name="type"> The type of the client service </param>
public abstract class GattClientService(
    BlePeripheral blePeripheral,
    BleUuid uuid,
    GattServiceType type,
    GattClientService? previousService,
    ILogger<GattClientService> logger
) : IGattClientService
{
    private readonly GattClientService? _previousService = previousService;
    private readonly List<GattClientCharacteristic> _characteristics = [];

    /// <summary> The optional logger </summary>
    protected ILogger<GattClientService> Logger { get; } = logger;

    /// <summary> The logger factory </summary>
    protected ILoggerFactory LoggerFactory => Peripheral.Device.LoggerFactory;

    /// <summary> The peripheral of the service </summary>
    public IBlePeripheral Peripheral { get; } = blePeripheral;

    /// <inheritdoc />
    public BleUuid Uuid { get; } = uuid;

    /// <inheritdoc />
    public GattServiceType Type { get; } = type;

    public virtual ushort StartHandle => _previousService?.EndHandle ?? 0;

    public virtual ushort EndHandle
    {
        get
        {
            ushort handleOffset = StartHandle;
            if (_characteristics.Count is 0)
                return (ushort)(handleOffset + 1);
            return (ushort)(_characteristics[^1].EndHandle + 1);
        }
    }

    /// <inheritdoc />
    public IReadOnlyCollection<IGattClientCharacteristic> Characteristics => _characteristics.AsReadOnly();

    /// <inheritdoc />
    public async Task<IGattClientCharacteristic> AddCharacteristicAsync(
        BleUuid uuid,
        GattProperty gattProperty,
        IGattClientAttribute.OnReadCallback? onRead,
        IGattClientAttribute.OnWriteCallback? onWrite,
        CancellationToken cancellationToken
    )
    {
        GattClientCharacteristic? previousCharacteristic = _characteristics.Count > 0 ? _characteristics[^1] : null;
        GattClientCharacteristic characteristic = await CreateCharacteristicAsyncCore(
                uuid,
                gattProperty,
                onRead,
                onWrite,
                previousCharacteristic,
                cancellationToken
            )
            .ConfigureAwait(false);
        _characteristics.Add(characteristic);
        return characteristic;
    }

    /// <summary> Called when creating a new characteristic </summary>
    /// <param name="uuid"> The UUID of the characteristic to create </param>
    /// <param name="gattProperty"> The property of the characteristic to create </param>
    /// <param name="onRead"> Callback when a read request was received </param>
    /// <param name="onWrite"> Callback when a write request was received </param>
    /// <param name="previousCharacteristic"> The characteristic before the current one </param>
    /// <param name="cancellationToken"> The CancellationToken to cancel the operation </param>
    /// <returns> A <see cref="IGattClientCharacteristic"/> </returns>
    protected abstract Task<GattClientCharacteristic> CreateCharacteristicAsyncCore(
        BleUuid uuid,
        GattProperty gattProperty,
        IGattClientAttribute.OnReadCallback? onRead,
        IGattClientAttribute.OnWriteCallback? onWrite,
        GattClientCharacteristic? previousCharacteristic,
        CancellationToken cancellationToken
    );
}
