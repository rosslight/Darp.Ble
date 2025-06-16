using System.Buffers.Binary;
using System.Reactive.Linq;
using Darp.Ble.Data;
using Darp.Ble.Gatt;
using Darp.Ble.Gatt.Server;
using Darp.Ble.Hci;
using Darp.Ble.Hci.Payload.Att;
using Microsoft.Extensions.Logging;

namespace Darp.Ble.HciHost.Gatt.Server;

internal sealed class HciHostGattServerService(
    BleUuid uuid,
    GattServiceType type,
    ushort attHandle,
    ushort endGroupHandle,
    HciHostGattServerPeer peer,
    ILogger<HciHostGattServerService> logger
) : GattServerService(peer, uuid, type, logger)
{
    private readonly ushort _attHandle = attHandle;
    private readonly ushort _endGroupHandle = endGroupHandle;
    public new HciHostGattServerPeer Peer { get; } = peer;

    protected override IObservable<GattServerCharacteristic> DiscoverCharacteristicsCore()
    {
        return Observable.Create<GattServerCharacteristic>(
            async (observer, token) =>
            {
                ushort startingHandle = _attHandle;
                HciHostGattServerCharacteristic? lastCharacteristic = null;
                List<HciHostGattServerCharacteristic> discoveredCharacteristics = [];
                while (!token.IsCancellationRequested && startingHandle < 0xFFFF)
                {
                    AttResponse<AttReadByTypeRsp> response = await Peer.QueryAttPduAsync<
                        AttReadByTypeReq<ushort>,
                        AttReadByTypeRsp
                    >(
                            new AttReadByTypeReq<ushort>
                            {
                                StartingHandle = startingHandle,
                                EndingHandle = _endGroupHandle,
                                AttributeType = 0x2803,
                            },
                            cancellationToken: token
                        )
                        .ConfigureAwait(false);
                    if (response.IsError)
                    {
                        if (response.Error.ErrorCode is AttErrorCode.AttributeNotFoundError)
                            break;
                        throw new Exception(
                            $"Could not discover characteristics due to error {response.Error.ErrorCode}"
                        );
                    }
                    AttReadByTypeRsp rsp = response.Value;
                    if (rsp.AttributeDataList.Length == 0)
                        break;
                    foreach ((ushort handle, ReadOnlyMemory<byte> memory) in rsp.AttributeDataList)
                    {
                        if (handle < startingHandle)
                            throw new Exception(
                                "Handle of discovered characteristic is smaller than starting handle of service"
                            );
                        var properties = (GattProperty)memory.Span[0];
                        ushort characteristicHandle = BinaryPrimitives.ReadUInt16LittleEndian(memory.Span[1..]);
                        var characteristicUuid = BleUuid.Read(memory.Span[3..]);
                        var characteristic = new HciHostGattServerCharacteristic(
                            this,
                            characteristicUuid,
                            characteristicHandle,
                            properties,
                            ServiceProvider.GetLogger<HciHostGattServerCharacteristic>()
                        );
                        discoveredCharacteristics.Add(characteristic);
                        if (lastCharacteristic is not null)
                            lastCharacteristic.EndHandle = handle;
                        lastCharacteristic = characteristic;
                    }
                    startingHandle = (ushort)(rsp.AttributeDataList[^1].Handle + 1);
                }
                if (lastCharacteristic is not null)
                    lastCharacteristic.EndHandle = _endGroupHandle;
                foreach (HciHostGattServerCharacteristic characteristic in discoveredCharacteristics)
                {
                    observer.OnNext(characteristic);
                }
            }
        );
    }

    protected override IObservable<GattServerCharacteristic> DiscoverCharacteristicsCore(BleUuid uuid)
    {
        return DiscoverCharacteristicsCore().Where(x => x.Uuid == uuid);
    }
}
