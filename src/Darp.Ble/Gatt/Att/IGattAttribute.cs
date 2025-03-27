using Darp.Ble.Data;
using Darp.Ble.Gatt.Client;

namespace Darp.Ble.Gatt.Att;

/// <summary> An interface defining a gatt attribute with a start handle and an end handle </summary>
#pragma warning disable CA1711 // Identifiers should not have incorrect suffix
public interface IGattAttribute
#pragma warning restore CA1711
{
    /// <summary> The type of the attribute </summary>
    BleUuid AttributeType { get; }

    /// <summary> The start handle of the attribute </summary>
    ushort Handle { get; }

    /// <summary> Check permissions before reading the value </summary>
    /// <param name="clientPeer"> The connected peer </param>
    /// <returns> <see cref="PermissionCheckStatus.Success"/>, if permissions are valid; A failure otherwise </returns>
    /// <seealso href="https://www.bluetooth.com/wp-content/uploads/Files/Specification/HTML/Core-60/out/en/host/attribute-protocol--att-.html#UUID-3cc21e8f-6324-ee15-5fb3-3cb98120ea4e"/>
    PermissionCheckStatus CheckReadPermissions(IGattClientPeer clientPeer);

    /// <summary> Check permissions before writing the value </summary>
    /// <param name="clientPeer"> The connected peer </param>
    /// <returns> <see cref="PermissionCheckStatus.Success"/>, if permissions are valid; A failure otherwise </returns>
    /// <seealso href="https://www.bluetooth.com/wp-content/uploads/Files/Specification/HTML/Core-60/out/en/host/attribute-protocol--att-.html#UUID-3cc21e8f-6324-ee15-5fb3-3cb98120ea4e"/>
    PermissionCheckStatus CheckWritePermissions(IGattClientPeer clientPeer);

    /// <summary> Get the current value of the characteristic </summary>
    /// <param name="clientPeer"> The client peer to get the value for </param>
    /// <param name="serviceProvider"> The service provider to resolve objects from </param>
    /// <returns> The current value </returns>
    ValueTask<byte[]> ReadValueAsync(IGattClientPeer? clientPeer, IServiceProvider serviceProvider);

    /// <summary> Update the characteristic value </summary>
    /// <param name="clientPeer"> The client peer to update the value for </param>
    /// <param name="value"> The value to update with </param>
    /// <param name="serviceProvider"> The service provider to resolve objects from </param>
    /// <returns> The status of the update operation </returns>
    ValueTask<GattProtocolStatus> WriteValueAsync(
        IGattClientPeer? clientPeer,
        byte[] value,
        IServiceProvider serviceProvider
    );
}

#region Delegate Defintions
#pragma warning disable MA0048

/// <summary> Defines the callback when the value should be read from the characteristic </summary>
/// <param name="clientPeer"> The client who issued the read request. If null, the request was caused not caused by a remote client but by the darp ble stack </param>
/// <returns> A valueTask which holds the bytes of the characteristic value when completed </returns>
public delegate ValueTask<byte[]> OnReadAsyncCallback(IGattClientPeer? clientPeer, IServiceProvider serviceProvider);

/// <summary> Defines the callback when the value should be read from the characteristic </summary>
/// <param name="clientPeer"> The client who issued the read request. If null, the request was caused not caused by a remote client but by the darp ble stack </param>
/// <returns> A valueTask which holds the characteristic value when completed </returns>
public delegate ValueTask<T> OnReadAsyncCallback<T>(IGattClientPeer? clientPeer, IServiceProvider serviceProvider);

/// <summary> Defines the callback when the value should be read from the characteristic </summary>
/// <param name="clientPeer"> The client who issued the read request. If null, the request was caused not caused by a remote client but by the darp ble stack </param>
/// <param name="value"> The value to be written to the characteristic </param>
/// <returns> A valueTask which holds the status of the write operation when completed </returns>
public delegate ValueTask<GattProtocolStatus> OnWriteAsyncCallback(
    IGattClientPeer? clientPeer,
    byte[] value,
    IServiceProvider serviceProvider
);

/// <summary> Defines the callback when the value should be read from the characteristic </summary>
/// <param name="clientPeer"> The client who issued the read request. If null, the request was caused not caused by a remote client but by the darp ble stack </param>
/// <param name="value"> The value to be written to the characteristic </param>
/// <returns> A valueTask which holds the status of the write operation when completed </returns>
public delegate ValueTask<GattProtocolStatus> OnWriteAsyncCallback<in T>(
    IGattClientPeer? clientPeer,
    T value,
    IServiceProvider serviceProvider
);

/// <summary> Defines the callback when the value should be read from the characteristic </summary>
/// <param name="clientPeer"> The client who issued the read request. If null, the request was caused not caused by a remote client but by the darp ble stack </param>
/// <returns> The attribute value </returns>
public delegate byte[] OnReadCallback(IGattClientPeer? clientPeer, IServiceProvider serviceProvider);

/// <summary> Defines the callback when the value should be read from the characteristic </summary>
/// <param name="clientPeer"> The client who issued the read request. If null, the request was caused not caused by a remote client but by the darp ble stack </param>
/// <returns> The attribute value </returns>
public delegate T OnReadCallback<out T>(IGattClientPeer? clientPeer, IServiceProvider serviceProvider);

/// <summary> Defines the callback when the value should be read from the characteristic </summary>
/// <param name="clientPeer"> The client who issued the read request. If null, the request was caused not caused by a remote client but by the darp ble stack </param>
/// <param name="value"> The value to be written to the characteristic </param>
/// <returns> The status of the write operation </returns>
public delegate GattProtocolStatus OnWriteCallback(
    IGattClientPeer? clientPeer,
    byte[] value,
    IServiceProvider serviceProvider
);

/// <summary> Defines the callback when the value should be read from the characteristic </summary>
/// <param name="clientPeer"> The client who issued the read request. If null, the request was caused not caused by a remote client but by the darp ble stack </param>
/// <param name="value"> The value to be written to the characteristic </param>
/// <returns> The status of the write operation </returns>
public delegate GattProtocolStatus OnWriteCallback<in T>(
    IGattClientPeer? clientPeer,
    T value,
    IServiceProvider serviceProvider
);
#endregion
