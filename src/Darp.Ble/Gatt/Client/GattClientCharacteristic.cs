using System.Diagnostics.CodeAnalysis;
using Darp.Ble.Data;
using Darp.Ble.Gatt.Att;
using Microsoft.Extensions.Logging;

namespace Darp.Ble.Gatt.Client;

internal static class Ex
{
    public static bool ContainsUuid<T>(this IEnumerable<T> source, BleUuid uuid)
        where T : IGattAttribute => source.Any(x1 => x1.AttributeType == uuid);

    public static bool TryGetByUuid<T>(this IEnumerable<T> source, BleUuid uuid, [NotNullWhen(true)] out T? value)
        where T : IGattAttribute
    {
        foreach (T x1 in source)
        {
            if (x1.AttributeType != uuid)
                continue;
            value = x1;
            return true;
        }
        value = default;
        return false;
    }
}

/// <summary> An abstract gatt client characteristic </summary>
/// <param name="clientService"> The parent client service </param>
/// <param name="properties"> The property of the characteristic </param>
/// <param name="value"> The characteristic value </param>
/// <param name="logger"> The logger of the characteristic </param>
public abstract class GattClientCharacteristic(
    GattClientService clientService,
    GattProperty properties,
    IGattCharacteristicValue value,
    ILogger<GattClientCharacteristic> logger
) : IGattClientCharacteristic
{
    private readonly List<IGattCharacteristicValue> _descriptors = [];

    /// <summary> The optional logger </summary>
    protected ILogger<GattClientCharacteristic> Logger { get; } = logger;

    /// <summary> The logger factory </summary>
    protected ILoggerFactory LoggerFactory => Service.Peripheral.Device.LoggerFactory;

    /// <inheritdoc />
    public IGattClientService Service { get; } = clientService;

    /// <inheritdoc />
    public BleUuid Uuid => Value.AttributeType;

    /// <inheritdoc />
    public GattProperty Properties { get; } = properties;

    /// <inheritdoc />
    public IReadOnlyCollection<IGattCharacteristicValue> Descriptors => _descriptors;

    /// <inheritdoc />
    public IGattCharacteristicDeclaration Declaration { get; } =
        new GattClientCharacteristicDeclaration(properties, clientService.Peripheral.GattDatabase, value);

    /// <inheritdoc />
    public IGattCharacteristicValue Value { get; } = value;

    /// <inheritdoc />
    public void AddDescriptor(IGattCharacteristicValue value)
    {
        ArgumentNullException.ThrowIfNull(value);
        if (Descriptors.ContainsUuid(value.AttributeType))
            throw new Exception($"Descriptor with type {value.AttributeType} was already added");
        _descriptors.Add(value);
        OnAddDescriptor(value);
        Service.Peripheral.GattDatabase.AddDescriptor(this, value);
    }

    /// <summary> Called after a new descriptor was added </summary>
    /// <param name="value"> The value of the new descriptor </param>
    protected virtual void OnAddDescriptor(IGattCharacteristicValue value) { }

    /*
    /// <inheritdoc />
    public ValueTask<byte[]> GetValueAsync(IGattClientPeer? clientPeer)
    {
        if (_onRead is null)
            throw new NotSupportedException("Reading is not supported by this characteristic");
        return _onRead(clientPeer);
    }

    public ValueTask<GattProtocolStatus> UpdateValueAsync(IGattClientPeer? clientPeer, ReadOnlyMemory<byte> value)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public ValueTask<GattProtocolStatus> UpdateValueAsync(IGattClientPeer? clientPeer, ReadOnlySpan<byte> value)
    {
        if (_onWrite is null)
            throw new NotSupportedException("Writing is not supported by this characteristic");
        return _onWrite(clientPeer, value);
    }
*/
    /// <inheritdoc />
    public void NotifyValue(IGattClientPeer? clientPeer, byte[] value)
    {
        if (clientPeer is not null)
        {
            if (Value.CheckReadPermissions(clientPeer) is PermissionCheckStatus.Success)
            {
                ValueTask<GattProtocolStatus> valueTask = Value.WriteValueAsync(clientPeer, value);
                if (!valueTask.IsCompletedSuccessfully)
                {
                    _ = valueTask.AsTask();
                }
            }
            NotifyCore(clientPeer, value);
        }
        else
        {
            foreach (IGattClientPeer connectedPeer in Service.Peripheral.PeerDevices.Values)
            {
                NotifyCore(connectedPeer, value);
            }
        }
    }

    /// <inheritdoc />
    public async Task IndicateAsync(IGattClientPeer? clientPeer, byte[] value, CancellationToken cancellationToken)
    {
        if (clientPeer is not null)
        {
            if (Value.CheckReadPermissions(clientPeer) is PermissionCheckStatus.Success)
            {
                await Value.WriteValueAsync(clientPeer, value).ConfigureAwait(false);
            }
            await IndicateAsyncCore(clientPeer, value, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            IEnumerable<Task> tasks = Service.Peripheral.PeerDevices.Values.Select(connectedPeer =>
                IndicateAsyncCore(connectedPeer, value, cancellationToken)
            );
            await Task.WhenAll(tasks).ConfigureAwait(false);
        }
    }

    /// <summary> Notify a connected clientPeer of a new value </summary>
    /// <param name="clientPeer"> The client to notify </param>
    /// <param name="value"> The value to be used </param>
    protected abstract void NotifyCore(IGattClientPeer clientPeer, byte[] value);

    /// <summary> Notify a connected clientPeer of a new value </summary>
    /// <param name="clientPeer"> The client to notify </param>
    /// <param name="value"> The value to be used </param>
    /// <param name="cancellationToken"> The cancellationToken to cancel the operation </param>
    /// <returns> A task with the result of the indication </returns>
    protected abstract Task IndicateAsyncCore(
        IGattClientPeer clientPeer,
        byte[] value,
        CancellationToken cancellationToken
    );

    /// <inheritdoc />
    public override string ToString() => $"Characteristic {Uuid}";
}

/// <summary> The implementation of a gatt client characteristic with a single property </summary>
/// <param name="characteristic"> The actual characteristic </param>
/// <typeparam name="TProp1"> The property </typeparam>
[SuppressMessage(
    "Design",
    "CA1033:Interface methods should be callable by child types",
    Justification = "Child classes should only be wrappers and should not call any methods"
)]
public class GattClientCharacteristic<TProp1>(IGattClientCharacteristic characteristic)
    : IGattClientCharacteristic<TProp1>
    where TProp1 : IBleProperty
{
    /// <summary> The underlying characteristic </summary>
    protected IGattClientCharacteristic Characteristic { get; } = characteristic;

    /// <inheritdoc />
    public IGattClientService Service => Characteristic.Service;

    /// <inheritdoc />
    public BleUuid Uuid => Characteristic.Uuid;

    /// <inheritdoc />
    public GattProperty Properties => Characteristic.Properties;

    /// <inheritdoc />
    public IReadOnlyCollection<IGattCharacteristicValue> Descriptors => Characteristic.Descriptors;

    IGattCharacteristicDeclaration IGattClientCharacteristic.Declaration => Characteristic.Declaration;
    IGattCharacteristicValue IGattClientCharacteristic.Value => Characteristic.Value;

    /// <inheritdoc />
    public void AddDescriptor(IGattCharacteristicValue value) => Characteristic.AddDescriptor(value);

    void IGattClientCharacteristic.NotifyValue(IGattClientPeer? clientPeer, byte[] value) =>
        Characteristic.NotifyValue(clientPeer, value);

    Task IGattClientCharacteristic.IndicateAsync(
        IGattClientPeer? clientPeer,
        byte[] value,
        CancellationToken cancellationToken
    ) => Characteristic.IndicateAsync(clientPeer, value, cancellationToken);
}

/// <summary> The implementation of a gatt client characteristic with a single property </summary>
/// <param name="characteristic"> The actual characteristic </param>
/// <typeparam name="TProp1"> The first property </typeparam>
/// <typeparam name="TProp2"> The second property </typeparam>
public sealed class GattClientCharacteristic<TProp1, TProp2>(IGattClientCharacteristic characteristic)
    : GattClientCharacteristic<TProp1>(characteristic),
        IGattClientCharacteristic<TProp2>
    where TProp1 : IBleProperty
    where TProp2 : IBleProperty
{
    /// <summary> Convert implicitly to a different order of type parameters </summary>
    /// <param name="characteristicDeclaration"> The characteristic declaration to convert </param>
    /// <returns> The converted characteristic declaration </returns>
    [SuppressMessage("Usage", "CA2225:Operator overloads have named alternates", Justification = "Convenience method")]
    public static implicit operator GattClientCharacteristic<TProp2, TProp1>(
        GattClientCharacteristic<TProp1, TProp2> characteristicDeclaration
    )
    {
        ArgumentNullException.ThrowIfNull(characteristicDeclaration);
        return new GattClientCharacteristic<TProp2, TProp1>(characteristicDeclaration.Characteristic);
    }
}
