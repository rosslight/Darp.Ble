using Darp.Ble.Data;

namespace Darp.Ble.Gatt.Client;

/// <summary> An abstract gatt client characteristic </summary>
/// <param name="uuid"> The UUID of the characteristic </param>
/// <param name="property"> The property of the characteristic </param>
public abstract class GattClientCharacteristic(BleUuid uuid, GattProperty property) : IGattClientCharacteristic
{
    /// <inheritdoc />
    public BleUuid Uuid { get; } = uuid;
    /// <inheritdoc />
    public GattProperty Property { get; } = property;

    /// <inheritdoc />
    public IDisposable OnWrite(Func<IGattClientPeer, byte[], CancellationToken, Task<GattProtocolStatus>> callback)
    {
        return OnWriteCore(callback);
    }

    /// <inheritdoc cref="OnWrite"/>
    protected abstract IDisposable OnWriteCore(Func<IGattClientPeer, byte[], CancellationToken, Task<GattProtocolStatus>> callback);

    /// <inheritdoc />
    public async Task<bool> NotifyAsync(IGattClientPeer clientPeer, byte[] source, CancellationToken cancellationToken)
    {
        return await NotifyAsyncCore(clientPeer, source, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc cref="NotifyAsync"/>
    protected abstract Task<bool> NotifyAsyncCore(IGattClientPeer clientPeer, byte[] source, CancellationToken cancellationToken);
}

/// <summary> The implementation of a gatt client characteristic with a single property </summary>
/// <param name="characteristic"> The actual characteristic </param>
/// <typeparam name="TProp1"> The property </typeparam>
public class GattClientCharacteristic<TProp1>(IGattClientCharacteristic characteristic)
    : IGattClientCharacteristic<TProp1>
    where TProp1 : IBleProperty
{
    /// <inheritdoc />
    public GattProperty Property => Characteristic.Property;
    /// <inheritdoc />
    public BleUuid Uuid => Characteristic.Uuid;
    /// <inheritdoc />
    public IGattClientCharacteristic Characteristic { get; } = characteristic;
}

/// <summary> The implementation of a gatt client characteristic with a single property </summary>
/// <param name="characteristic"> The actual characteristic </param>
/// <typeparam name="TProp1"> The first property </typeparam>
/// <typeparam name="TProp2"> The second property </typeparam>
public sealed class GattClientCharacteristic<TProp1, TProp2>(IGattClientCharacteristic characteristic)
    : GattClientCharacteristic<TProp1>(characteristic), IGattClientCharacteristic<TProp2>
    where TProp1 : IBleProperty
    where TProp2 : IBleProperty;

/// <summary> The implementation of a gatt client characteristic with a single property </summary>
/// <param name="characteristic"> The actual characteristic </param>
/// <typeparam name="T"> The type of the characteristic value </typeparam>
/// <typeparam name="TProp1"> The property </typeparam>
public class GattTypedClientCharacteristic<T, TProp1>(IGattClientCharacteristic characteristic)
    : IGattTypedClientCharacteristic<T, TProp1>
    where T : unmanaged
    where TProp1 : IBleProperty
{
    /// <inheritdoc />
    public GattProperty Property => Characteristic.Property;
    /// <inheritdoc />
    public BleUuid Uuid => Characteristic.Uuid;
    /// <inheritdoc />
    public IGattClientCharacteristic Characteristic { get; } = characteristic;
}

/// <summary> The implementation of a gatt client characteristic with a single property </summary>
/// <param name="characteristic"> The actual characteristic </param>
/// <typeparam name="T"> The type of the characteristic value </typeparam>
/// <typeparam name="TProp1"> The first property </typeparam>
/// <typeparam name="TProp2"> The second property </typeparam>
public sealed class GattTypedClientCharacteristic<T, TProp1, TProp2>(IGattClientCharacteristic characteristic)
    : GattTypedClientCharacteristic<T, TProp1>(characteristic), IGattTypedClientCharacteristic<T, TProp2>
    where T : unmanaged
    where TProp1 : IBleProperty
    where TProp2 : IBleProperty;