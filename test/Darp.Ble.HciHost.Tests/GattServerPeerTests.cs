using System.Buffers.Binary;
using Darp.Ble.Data;
using Darp.Ble.Gatt.Server;
using Darp.Ble.Hci.Payload.Att;
using Darp.Ble.HciHost.Gatt.Server;
using Darp.Ble.HciHost.Verify;
using Shouldly;

namespace Darp.Ble.HciHost.Tests;

public sealed class GattServerPeerTests
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Fact(Timeout = 5000)]
    public async Task DiscoverServicesAsync_OneServiceEndWithNotFound()
    {
        const ushort connectionHandle = 0x001;
        BleUuid expectedUuid = BleUuid.FromUInt16(0x180D);

        HciMessage[] messages =
        [
            HciMessages.AttReadByGroupTypeResponse(
                connectionHandle,
                new AttGroupTypeData(0x0001, 0x0005, expectedUuid.ToByteArray())
            ),
            HciMessages.AttNotFoundErrorResponse(connectionHandle, AttOpCode.ATT_READ_BY_GROUP_TYPE_REQ, 0x0006),
        ];
        (HciHostGattServerPeer peer, ReplayTransportLayer replay) = await Helpers.CreateConnectedServerPeerAsync(
            connectionHandle: connectionHandle,
            additionalControllerMessages: messages,
            token: Token
        );

        await peer.DiscoverServicesAsync(Token);

        peer.Services.Count.ShouldBe(1);
        var service = peer.Services.Single().ShouldBeOfType<HciHostGattServerService>();
        service.Uuid.ShouldBe(expectedUuid);

        await Verifier.Verify(new { replay.MessagesToController, replay.MessagesToHost });
    }

    [Fact(Timeout = 5000)]
    public async Task DiscoverServicesAsync_MultipleServicesAcrossMultipleResponses()
    {
        const ushort connectionHandle = 0x001;
        BleUuid gapServiceUuid = BleUuid.FromUInt16(0x1800);
        BleUuid heartRateServiceUuid = BleUuid.FromUInt16(0x180D);
        BleUuid batteryServiceUuid = BleUuid.FromUInt16(0x180F);
        BleUuid deviceInformationServiceUuid = BleUuid.FromUInt16(0x180A);

        HciMessage[] messages =
        [
            // First response with GAP service and first custom service
            HciMessages.AttReadByGroupTypeResponse(
                connectionHandle,
                new AttGroupTypeData(0x0001, 0x0005, gapServiceUuid.ToByteArray()),
                new AttGroupTypeData(0x0006, 0x000A, heartRateServiceUuid.ToByteArray())
            ),
            // Second response with remaining services (starting from handle 0x000B)
            HciMessages.AttReadByGroupTypeResponse(
                connectionHandle,
                new AttGroupTypeData(0x000B, 0x000F, batteryServiceUuid.ToByteArray()),
                new AttGroupTypeData(0x0010, 0x0015, deviceInformationServiceUuid.ToByteArray())
            ),
            // End with NotFound error
            HciMessages.AttNotFoundErrorResponse(connectionHandle, AttOpCode.ATT_READ_BY_GROUP_TYPE_REQ, 0x0016),
        ];
        (HciHostGattServerPeer peer, ReplayTransportLayer replay) = await Helpers.CreateConnectedServerPeerAsync(
            connectionHandle: connectionHandle,
            additionalControllerMessages: messages,
            token: Token
        );

        await peer.DiscoverServicesAsync(Token);

        peer.Services.Count.ShouldBe(4);
        peer.Services[0].ShouldBeOfType<HciHostGattServerService>();
        peer.Services[0].Uuid.ShouldBe(gapServiceUuid);
        peer.Services[1].ShouldBeOfType<HciHostGattServerService>();
        peer.Services[1].Uuid.ShouldBe(heartRateServiceUuid);
        peer.Services[2].ShouldBeOfType<HciHostGattServerService>();
        peer.Services[2].Uuid.ShouldBe(batteryServiceUuid);
        peer.Services[3].ShouldBeOfType<HciHostGattServerService>();
        peer.Services[3].Uuid.ShouldBe(deviceInformationServiceUuid);

        await Verifier.Verify(new { replay.MessagesToController, replay.MessagesToHost });
    }

    [Fact(Timeout = 5000)]
    public async Task DiscoverCharacteristicsAsync_OneCharacteristicEndWithNotFound()
    {
        const ushort connectionHandle = 0x001;
        BleUuid heartRateServiceUuid = BleUuid.FromUInt16(0x180D);
        BleUuid heartRateMeasurementCharacteristicUuid = BleUuid.FromUInt16(0x2A37);
        const GattProperty properties = GattProperty.Notify;
        const ushort serviceHandle = 0x0001;
        const ushort serviceEndHandle = 0x0005;
        const ushort characteristicDeclarationHandle = 0x0002;
        const ushort characteristicValueHandle = 0x0003;

        // Build characteristic value: [properties (1)] [value handle (2)] [UUID (2)]
        var characteristicValue = new byte[5];
        characteristicValue[0] = (byte)properties;
        BinaryPrimitives.WriteUInt16LittleEndian(characteristicValue.AsSpan(1), characteristicValueHandle);
        heartRateMeasurementCharacteristicUuid.ToByteArray().CopyTo(characteristicValue.AsSpan(3));

        HciMessage[] messages =
        [
            // Service discovery
            HciMessages.AttReadByGroupTypeResponse(
                connectionHandle,
                new AttGroupTypeData(serviceHandle, serviceEndHandle, heartRateServiceUuid.ToByteArray())
            ),
            HciMessages.AttNotFoundErrorResponse(connectionHandle, AttOpCode.ATT_READ_BY_GROUP_TYPE_REQ, 0x0006),
            // Characteristic discovery
            HciMessages.AttReadByTypeResponse(
                connectionHandle,
                new AttReadByTypeData(characteristicDeclarationHandle, characteristicValue)
            ),
            HciMessages.AttNotFoundErrorResponse(
                connectionHandle,
                AttOpCode.ATT_READ_BY_TYPE_REQ,
                characteristicValueHandle + 1
            ),
        ];
        (HciHostGattServerPeer peer, ReplayTransportLayer replay) = await Helpers.CreateConnectedServerPeerAsync(
            connectionHandle: connectionHandle,
            additionalControllerMessages: messages,
            token: Token
        );

        await peer.DiscoverServicesAsync(Token);
        var service = peer.Services.Single().ShouldBeOfType<HciHostGattServerService>();
        await service.DiscoverCharacteristicsAsync(Token);

        service.Characteristics.Count.ShouldBe(1);
        var char0 = service.Characteristics.Single().ShouldBeOfType<HciHostGattServerCharacteristic>();
        char0.Uuid.ShouldBe(heartRateMeasurementCharacteristicUuid);
        char0.Properties.ShouldBe(properties);
        char0.AttributeHandle.ShouldBe(characteristicValueHandle);

        await Verifier.Verify(new { replay.MessagesToController, replay.MessagesToHost });
    }

    [Fact(Timeout = 5000)]
    public async Task DiscoverCharacteristicsAsync_MultipleCharacteristicsAcrossMultipleResponses()
    {
        const ushort connectionHandle = 0x001;
        BleUuid deviceInformationServiceUuid = BleUuid.FromUInt16(0x180A);
        BleUuid manufacturerNameStringCharacteristicUuid = BleUuid.FromUInt16(0x2A29);
        BleUuid modelNumberStringCharacteristicUuid = BleUuid.FromUInt16(0x2A24);
        BleUuid serialNumberStringCharacteristicUuid = BleUuid.FromUInt16(0x2A25);
        const GattProperty char1Properties = GattProperty.Read;
        const GattProperty char2Properties = GattProperty.Read;
        const GattProperty char3Properties = GattProperty.Read;
        const ushort serviceHandle = 0x0001;
        const ushort serviceEndHandle = 0x000C;
        const ushort char1DeclarationHandle = 0x0002;
        const ushort char1ValueHandle = 0x0003;
        const ushort char2DeclarationHandle = 0x0005;
        const ushort char2ValueHandle = 0x0006;
        const ushort char3DeclarationHandle = 0x0008;
        const ushort char3ValueHandle = 0x0009;

        // Build characteristic values
        byte[] char1Value = new byte[5];
        char1Value[0] = (byte)char1Properties;
        BinaryPrimitives.WriteUInt16LittleEndian(char1Value.AsSpan(1), char1ValueHandle);
        manufacturerNameStringCharacteristicUuid.ToByteArray().CopyTo(char1Value.AsSpan(3));

        byte[] char2Value = new byte[5];
        char2Value[0] = (byte)char2Properties;
        BinaryPrimitives.WriteUInt16LittleEndian(char2Value.AsSpan(1), char2ValueHandle);
        modelNumberStringCharacteristicUuid.ToByteArray().CopyTo(char2Value.AsSpan(3));

        byte[] char3Value = new byte[5];
        char3Value[0] = (byte)char3Properties;
        BinaryPrimitives.WriteUInt16LittleEndian(char3Value.AsSpan(1), char3ValueHandle);
        serialNumberStringCharacteristicUuid.ToByteArray().CopyTo(char3Value.AsSpan(3));

        HciMessage[] messages =
        [
            // Service discovery
            HciMessages.AttReadByGroupTypeResponse(
                connectionHandle,
                new AttGroupTypeData(serviceHandle, serviceEndHandle, deviceInformationServiceUuid.ToByteArray())
            ),
            HciMessages.AttNotFoundErrorResponse(connectionHandle, AttOpCode.ATT_READ_BY_GROUP_TYPE_REQ, 0x000D),
            // Characteristic discovery - first two in first response
            HciMessages.AttReadByTypeResponse(
                connectionHandle,
                new AttReadByTypeData(char1DeclarationHandle, char1Value),
                new AttReadByTypeData(char2DeclarationHandle, char2Value)
            ),
            // Third characteristic in second response
            HciMessages.AttReadByTypeResponse(
                connectionHandle,
                new AttReadByTypeData(char3DeclarationHandle, char3Value)
            ),
            HciMessages.AttNotFoundErrorResponse(
                connectionHandle,
                AttOpCode.ATT_READ_BY_TYPE_REQ,
                char3ValueHandle + 1
            ),
        ];
        (HciHostGattServerPeer peer, ReplayTransportLayer replay) = await Helpers.CreateConnectedServerPeerAsync(
            connectionHandle: connectionHandle,
            additionalControllerMessages: messages,
            token: Token
        );

        await peer.DiscoverServicesAsync(Token);
        var service = peer.Services.Single().ShouldBeOfType<HciHostGattServerService>();
        await service.DiscoverCharacteristicsAsync(Token);

        IGattServerCharacteristic[] characteristics = service.Characteristics.ToArray();
        characteristics.Length.ShouldBe(3);
        var char0 = characteristics[0].ShouldBeOfType<HciHostGattServerCharacteristic>();
        characteristics[0].Uuid.ShouldBe(manufacturerNameStringCharacteristicUuid);
        characteristics[0].Properties.ShouldBe(char1Properties);
        characteristics[0].AttributeHandle.ShouldBe(char1ValueHandle);
        characteristics[1].ShouldBeOfType<HciHostGattServerCharacteristic>();
        characteristics[1].Uuid.ShouldBe(modelNumberStringCharacteristicUuid);
        characteristics[1].Properties.ShouldBe(char2Properties);
        characteristics[1].AttributeHandle.ShouldBe(char2ValueHandle);
        characteristics[2].ShouldBeOfType<HciHostGattServerCharacteristic>();
        characteristics[2].Uuid.ShouldBe(serialNumberStringCharacteristicUuid);
        characteristics[2].Properties.ShouldBe(char3Properties);
        characteristics[2].AttributeHandle.ShouldBe(char3ValueHandle);

        await Verifier.Verify(new { replay.MessagesToController, replay.MessagesToHost });
    }
}
