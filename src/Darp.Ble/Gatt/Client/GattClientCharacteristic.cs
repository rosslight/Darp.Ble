using System.Diagnostics.CodeAnalysis;
using Darp.Ble.Data;
using Darp.Ble.Gatt.Att;
using Microsoft.Extensions.Logging;

namespace Darp.Ble.Gatt.Client;

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
    private readonly AttributeCollection<IGattCharacteristicValue> _descriptors = new(descriptor =>
        descriptor.AttributeType
    );

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
    public IReadonlyAttributeCollection<IGattCharacteristicValue> Descriptors => _descriptors;

    /// <inheritdoc />
    public IGattCharacteristicDeclaration Declaration { get; } =
        new GattClientCharacteristicDeclaration(properties, clientService.Peripheral.GattDatabase, value);

    /// <inheritdoc />
    public IGattCharacteristicValue Value { get; } = value;

    /// <inheritdoc />
    public void AddDescriptor(IGattCharacteristicValue value)
    {
        ArgumentNullException.ThrowIfNull(value);
        if (Descriptors.ContainsAny(value.AttributeType))
            throw new Exception($"Descriptor with type {value.AttributeType} was already added");
        _descriptors.Add(value);
        Service.Peripheral.GattDatabase.AddDescriptor(this, value);
        OnDescriptorAdded(value);
    }

    /// <summary> Called after a new descriptor was added </summary>
    /// <param name="value"> The value of the new descriptor </param>
    protected virtual void OnDescriptorAdded(IGattCharacteristicValue value) { }

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
    public async ValueTask NotifyValueAsync(IGattClientPeer? clientPeer, byte[] value)
    {
        if (clientPeer is not null)
        {
            if (Value.CheckReadPermissions(clientPeer) is PermissionCheckStatus.Success)
            {
                await Value.WriteValueAsync(clientPeer, value).ConfigureAwait(false);
            }
            await NotifyAsyncCore(clientPeer, value).ConfigureAwait(false);
        }
        else
        {
            foreach (IGattClientPeer connectedPeer in Service.Peripheral.PeerDevices.Values)
            {
                if (Value.CheckReadPermissions(connectedPeer) is PermissionCheckStatus.Success)
                {
                    await Value.WriteValueAsync(clientPeer, value).ConfigureAwait(false);
                }
                await NotifyAsyncCore(connectedPeer, value).ConfigureAwait(false);
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
            foreach (IGattClientPeer connectedPeer in Service.Peripheral.PeerDevices.Values)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (Value.CheckReadPermissions(connectedPeer) is PermissionCheckStatus.Success)
                {
                    ValueTask<GattProtocolStatus> valueTask = Value.WriteValueAsync(clientPeer, value);
                    if (!valueTask.IsCompletedSuccessfully)
                    {
                        _ = valueTask.AsTask();
                    }
                }
                await IndicateAsyncCore(connectedPeer, value, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    /// <summary> Notify a connected clientPeer of a new value </summary>
    /// <param name="clientPeer"> The client to notify </param>
    /// <param name="value"> The value to be used </param>
    protected abstract ValueTask NotifyAsyncCore(IGattClientPeer clientPeer, byte[] value);

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
    public IReadonlyAttributeCollection<IGattCharacteristicValue> Descriptors => Characteristic.Descriptors;

    IGattCharacteristicDeclaration IGattClientCharacteristic.Declaration => Characteristic.Declaration;
    IGattCharacteristicValue IGattClientCharacteristic.Value => Characteristic.Value;

    /// <inheritdoc />
    public void AddDescriptor(IGattCharacteristicValue value) => Characteristic.AddDescriptor(value);

    ValueTask IGattClientCharacteristic.NotifyValueAsync(IGattClientPeer? clientPeer, byte[] value) =>
        Characteristic.NotifyValueAsync(clientPeer, value);

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
