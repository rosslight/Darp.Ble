using System.Reactive.Linq;
using Darp.Ble.Data;
using Darp.Ble.Gatt.Server;
using Microsoft.Extensions.Logging;

namespace Darp.Ble.Mock.Testing;

/// <summary> Mocking utilities for GATT server services. </summary>
public static class MockHelpers
{
    /// <summary> Creates a mocked GATT server peer device. </summary>
    /// <param name="peripheralBuilder">A function that builds the peripheral.</param>
    /// <param name="loggerFactory">The logger factory to use. If null, no logging will be done.</param>
    /// <returns>The mocked GATT server peer device.</returns>
    /// <remarks> The mock will add a peripheral to the manager with a known address and connect to it. </remarks>
    public static async Task<IGattServerPeer> CreateMockedPeerDevice(
        Func<IBlePeripheral, Task> peripheralBuilder,
        ILoggerFactory? loggerFactory = null
    )
    {
        var address = BleAddress.NewRandomStaticAddress();
        BleManager manager = new BleManagerBuilder()
            .AddMock(factory =>
                factory.AddPeripheral(async device =>
                {
                    await peripheralBuilder(device.Peripheral).ConfigureAwait(false);
                    await device.SetRandomAddressAsync(address).ConfigureAwait(false);
                })
            )
            .SetLogger(loggerFactory)
            .CreateManager();
        IBleDevice device = manager.EnumerateDevices().First();
        await device.InitializeAsync().ConfigureAwait(false);
        return await device.Central.ConnectToPeripheral(address).FirstAsync();
    }

    /// <summary> Creates a mocked GATT server peer device. </summary>
    /// <param name="peripheralBuilder">A function that builds the peripheral.</param>
    /// <param name="loggerFactory">The logger factory to use. If null, no logging will be done.</param>
    /// <returns>The mocked GATT server peer device.</returns>
    /// <remarks> The mock will add a peripheral to the manager with a known address and connect to it. </remarks>
    public static Task<IGattServerPeer> CreateMockedPeerDevice(
        Action<IBlePeripheral> peripheralBuilder,
        ILoggerFactory? loggerFactory = null
    ) =>
        CreateMockedPeerDevice(
            peripheral =>
            {
                peripheralBuilder(peripheral);
                return Task.CompletedTask;
            },
            loggerFactory
        );

    /// <summary> Creates a mocked GATT server peer device. </summary>
    /// <typeparam name="T">The type of the service to create.</typeparam>
    /// <param name="peripheralBuilder">A function that builds the peripheral.</param>
    /// <param name="serviceSelector">A function that selects the service.</param>
    /// <param name="loggerFactory">The logger factory to use. If null, no logging will be done.</param>
    /// <returns>The mocked GATT server peer device.</returns>
    /// <remarks> The mock will add a peripheral to the manager with a known address and connect to it. </remarks>
    public static async Task<T> CreateMockedService<T>(
        Action<IBlePeripheral> peripheralBuilder,
        Func<IGattServerPeer, Task<T>> serviceSelector,
        ILoggerFactory? loggerFactory = null
    )
        where T : IGattServerService
    {
        ArgumentNullException.ThrowIfNull(serviceSelector);
        IGattServerPeer peer = await CreateMockedPeerDevice(peripheralBuilder, loggerFactory).ConfigureAwait(false);
        return await serviceSelector(peer).ConfigureAwait(false);
    }
}
