using System.Buffers.Binary;
using System.Reactive.Linq;
using Darp.Ble.Data;
using Darp.Ble.Gatt.Server;
using Darp.Ble.Hci.Package;
using Darp.Ble.Hci.Payload.Att;
using Microsoft.Extensions.Logging;

namespace Darp.Ble.HciHost.Gatt.Server;

internal sealed class HciHostGattServerService(BleUuid uuid, ushort attHandle, ushort endGroupHandle,
    HciHostGattServerPeer serverPeer, ILogger<HciHostGattServerService> logger) : GattServerService(serverPeer, uuid, logger)
{
    private readonly ushort _attHandle = attHandle;
    private readonly ushort _endGroupHandle = endGroupHandle;
    private readonly HciHostGattServerPeer _serverPeer = serverPeer;

    protected override IObservable<IGattServerCharacteristic> DiscoverCharacteristicsCore()
    {
         return Observable.Create<IGattServerCharacteristic>(async (observer, token) =>
        {
            ushort startingHandle = _attHandle;
            HciHostGattServerCharacteristic? lastCharacteristic = null;
            List<HciHostGattServerCharacteristic> discoveredCharacteristics = [];
            while (!token.IsCancellationRequested && startingHandle < 0xFFFF)
            {
                AttReadResult response = await _serverPeer.QueryAttPduAsync<AttReadByTypeReq<ushort>, AttReadByTypeRsp>(
                    new AttReadByTypeReq<ushort>
                {
                    StartingHandle = startingHandle,
                    EndingHandle = _endGroupHandle,
                    AttributeType = 0x2803,
                }, cancellationToken: token).ConfigureAwait(false);
                if (response.OpCode is AttOpCode.ATT_ERROR_RSP
                    && AttErrorRsp.TryReadLittleEndian(response.Pdu, out AttErrorRsp errorRsp, out _))
                {
                    if (errorRsp.ErrorCode is AttErrorCode.AttributeNotFoundError) break;
                    throw new Exception($"Could not discover characteristics due to error {errorRsp.ErrorCode}");
                }
                if (!(response.OpCode is AttOpCode.ATT_READ_BY_TYPE_RSP
                     && AttReadByTypeRsp.TryReadLittleEndian(response.Pdu, out AttReadByTypeRsp rsp, out _)))
                {
                    throw new Exception($"Received unexpected att response {response.OpCode}");
                }
                if (rsp.AttributeDataList.Length == 0) break;
                foreach ((ushort handle, ReadOnlyMemory<byte> memory) in rsp.AttributeDataList)
                {
                    if (handle < startingHandle)
                        throw new Exception("Handle of discovered characteristic is smaller than starting handle of service");
                    var properties = (GattProperty)memory.Span[0];
                    ushort characteristicHandle = BinaryPrimitives.ReadUInt16LittleEndian(memory.Span[1..]);
                    var characteristicUuid = new BleUuid(memory.Span[3..]);
                    var characteristic = new HciHostGattServerCharacteristic(this,
                        _serverPeer,
                        characteristicUuid,
                        characteristicHandle,
                        properties,
                        LoggerFactory.CreateLogger<HciHostGattServerCharacteristic>());
                    discoveredCharacteristics.Add(characteristic);
                    if (lastCharacteristic is not null) lastCharacteristic.EndHandle = handle;
                    lastCharacteristic = characteristic;
                }
                startingHandle = (ushort)(rsp.AttributeDataList[^1].Handle + 1);
            }
            if (lastCharacteristic is not null) lastCharacteristic.EndHandle = _endGroupHandle;
            foreach (HciHostGattServerCharacteristic characteristic in discoveredCharacteristics)
            {
                if (!await characteristic.DiscoverAllDescriptorsAsync(token))
                {
                    Logger?.LogWarning("Could not discover descriptors of characteristic {@Characteristic}", characteristic);
                    continue;
                }
                observer.OnNext(characteristic);
            }
        });
    }

    protected override IObservable<IGattServerCharacteristic> DiscoverCharacteristicsCore(BleUuid uuid)
    {
        return DiscoverCharacteristicsCore().Where(x => x.Uuid == uuid);
    }
}