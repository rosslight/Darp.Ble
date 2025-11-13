using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using Darp.Ble.Data;
using Darp.Ble.Gatt.Server;
using Darp.Ble.Hci.Package;
using Darp.Ble.Hci.Payload.Event;
using Darp.Ble.Hci.Transport;
using Darp.Ble.HciHost.Gatt.Server;
using Darp.Ble.HciHost.Verify;
using Microsoft.Extensions.Logging;

namespace Darp.Ble.HciHost.Tests;

public static class Helpers
{
    public static async Task<IBleDevice> GetAndInitializeBleDeviceAsync(
        ITransportLayer transportLayer,
        BleAddress? deviceAddress = null,
        CancellationToken token = default
    )
    {
        deviceAddress ??= BleAddress.CreateRandomAddress((UInt48)0xE0C5AA968B6E);
        IBleDevice device = await GetBleDeviceAsync(transportLayer, deviceAddress, token);
        await device.InitializeAsync(token);
        return device;
    }

    public static async Task<IBleDevice> GetBleDeviceAsync(
        ITransportLayer transportLayer,
        BleAddress? deviceAddress = null,
        CancellationToken token = default
    )
    {
        deviceAddress ??= BleAddress.CreateRandomAddress((UInt48)0xE0C5AA968B6E);
        ILoggerFactory loggerFactory = LoggerFactory.Create(builder =>
            builder.AddConsole().SetMinimumLevel(LogLevel.Trace)
        );
        BleManager manager = new BleManagerBuilder()
            .AddHciHost(
                transportLayer,
                factory =>
                {
                    factory.Settings = factory.Settings with
                    {
                        DefaultHciCommandTimeoutMs = 100,
                        DefaultAttTimeoutMs = 100,
                    };
                }
            )
            .SetLogger(loggerFactory)
            .CreateManager();
        IBleDevice device = manager.EnumerateDevices().First();
        await device.SetRandomAddressAsync(deviceAddress, token);
        return device;
    }

    internal static async Task<(HciHostGattServerPeer Peer, ReplayTransportLayer Replay)> CreateConnectedPeerAsync(
        BleAddress? peerAddress = null,
        ushort connectionHandle = 0x0001,
        IEnumerable<HciMessage>? additionalControllerMessages = null,
        CancellationToken token = default
    )
    {
        BleAddress resolvedPeerAddress = peerAddress ?? BleAddress.CreateRandomAddress((UInt48)0x112233445566);

        var responses = new List<HciMessage>(ReplayTransportLayer.InitializeBleDeviceMessages)
        {
            HciMessages.HciLeExtendedCreateConnectionCommandStatusEvent(),
            HciMessages.HciLeReadPhyEvent(connectionHandle, txPhy: 0x01, rxPhy: 0x01),
        };

        if (additionalControllerMessages is not null)
            responses.AddRange(additionalControllerMessages);

        responses.Add(HciMessages.HciDisconnectionCompleteEvent(connectionHandle));

        ReplayTransportLayer? replay = null;
        replay = new ReplayTransportLayer(
            (_, i) =>
            {
                (HciMessage? Message, TimeSpan) x = ReplayTransportLayer.IterateHciMessages(responses, i);
                if (x.Message is { Type: HciPacketType.HciAclData })
                {
                    // ReSharper disable once AccessToModifiedClosure
                    replay?.Push(HciMessages.HciNumberOfCompletedPacketsEvent(connectionHandle));
                }
                return x;
            },
            ReplayTransportLayer.InitializeBleDeviceMessages.Length,
            logger: null
        );
        IBleDevice device = await GetAndInitializeBleDeviceAsync(replay, token: token);

        Task<IGattServerPeer> peerTask = device
            .Central.ConnectToPeripheral(resolvedPeerAddress)
            .FirstAsync()
            .ToTask(token);

        replay.Push(
            HciMessages.HciLeEnhancedConnectionCompleteEvent(
                connectionHandle,
                HciLeConnectionRole.Central,
                (byte)resolvedPeerAddress.Type,
                resolvedPeerAddress.Value,
                connectionInterval: 0x0028,
                peripheralLatency: 0x0000,
                supervisionTimeout: 0x01F4,
                centralClockAccuracy: 0x01
            )
        );

        var peer = (HciHostGattServerPeer)await peerTask.ConfigureAwait(false);
        return (peer, replay);
    }
}
