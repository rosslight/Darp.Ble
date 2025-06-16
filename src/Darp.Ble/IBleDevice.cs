using Darp.Ble.Data;
using Darp.Ble.Data.AssignedNumbers;
using Darp.Ble.Exceptions;
using Darp.Ble.Gatt.Services;

namespace Darp.Ble;

/// <summary> The base interface of a ble device. </summary>
public interface IBleDevice : IAsyncDisposable
{
    /// <summary> The service provider used to retrieve services from </summary>
    protected internal IServiceProvider ServiceProvider { get; }

    /// <summary> True if the device was successfully initialized </summary>
    public bool IsInitialized { get; }

    /// <summary> True, if the device was disposed; False otherwise </summary>
    public bool IsDisposed { get; }

    /// <summary> Get an implementation specific identification string </summary>
    public string Identifier { get; }

    /// <summary> An optional name </summary>
    public string? Name { get; set; }

    /// <summary> An optional appearance of the device </summary>
    /// <remarks> The default value depends on the implementation. </remarks>
    /// <remarks> When set, make sure to notify depending services (e.g. <see cref="GapServiceContract"/> or the advertising data) of the change </remarks>
    /// <exception cref="NotSupportedException"> Some implementations might not support setting the appearance </exception>
    public AppearanceValues Appearance { get; set; }

    /// <summary>
    /// Gives back capabilities of this device. Before the device was successfully initialized, the capabilities are unknown
    /// </summary>
    Capabilities Capabilities { get; }

    /// <summary> Returns a view of the device in Observer Role </summary>
    /// <exception cref="NotInitializedException"> Thrown when the device has not been initialized </exception>
    /// <exception cref="NotSupportedException"> Thrown when the role is not supported </exception>
    IBleObserver Observer { get; }

    /// <summary> Returns a view of the device in Central Role </summary>
    /// <exception cref="NotInitializedException"> Thrown when the device has not been initialized </exception>
    /// <exception cref="NotSupportedException"> Thrown when the role is not supported </exception>
    IBleCentral Central { get; }

    /// <summary> Returns a view of the device in Broadcaster Role </summary>
    /// <exception cref="NotInitializedException"> Thrown when the device has not been initialized </exception>
    /// <exception cref="NotSupportedException"> Thrown when the role is not supported </exception>
    IBleBroadcaster Broadcaster { get; }

    /// <summary> Returns a view of the device in Peripheral Role </summary>
    /// <exception cref="NotInitializedException"> Thrown when the device has not been initialized </exception>
    /// <exception cref="NotSupportedException"> Thrown when the role is not supported </exception>
    IBlePeripheral Peripheral { get; }

    /// <summary> The random address of the device </summary>
    /// <remarks> Settable by calling <see cref="SetRandomAddressAsync"/> </remarks>
    BleAddress RandomAddress { get; }

    /// <summary> Initializes the ble device </summary>
    /// <param name="cancellationToken"> The cancellation token to cancel the operation </param>
    /// <returns> Success or a custom error code </returns>
    Task<InitializeResult> InitializeAsync(CancellationToken cancellationToken = default);

    /// <summary> Sets the random address of the device </summary>
    /// <param name="randomAddress"> The new, random address </param>
    /// <param name="cancellationToken"> The cancellation token to cancel the operation </param>
    /// <returns> A task that completes when the address was updated </returns>
    /// <remarks> Behavior when setting an address with Non-Random <see cref="BleAddressType"/> is not specified </remarks>
    Task SetRandomAddressAsync(BleAddress randomAddress, CancellationToken cancellationToken = default);
}
