using Darp.Ble.Connections;
using Darp.Ble.Gatt;
using Darp.Ble.Hci.Package;
using Darp.Ble.Hci.Payload.Att;
using Darp.Ble.HciHost.Verify;
using Microsoft.Extensions.Time.Testing;

namespace Darp.Ble.HciHost.Tests.Scenarios;

/// <summary> The peripheral as a fake peripheral behind a fake controller </summary>
internal sealed class HciHostPeer : IScenarioPeer
{
    private readonly FakeController _controller;
    private readonly FakePeripheral _peripheral;
    private readonly BleAdapter _adapter;
    private readonly ushort _value;

    private HciHostPeer(
        FakeController controller,
        FakePeripheral peripheral,
        BleAdapter adapter,
        FakeTimeProvider time,
        ConnectedPeripheral connection,
        RemoteGattCharacteristic characteristic,
        ushort value
    )
    {
        _controller = controller;
        _peripheral = peripheral;
        _adapter = adapter;
        Time = time;
        Connection = connection;
        Characteristic = characteristic;
        _value = value;
    }

    public static async Task<IScenarioPeer> ConnectAsync()
    {
        CancellationToken token = TestContext.Current.CancellationToken;
        var controller = new FakeController();
        var time = new FakeTimeProvider();
        ushort value = 0;
        FakePeripheral peripheral = new FakePeripheral(controller, 0x0001).AddService(
            0x180F,
            service => value = service.AddCharacteristic(0xFF01, GattProperty.Read | GattProperty.Write, [0])
        );
        BleAdapter adapter = await HciAdapter.OpenAsync(controller, time);
        ConnectedPeripheral connection = await HciAdapter.ConnectAsync(adapter, controller, peripheral);
        RemoteGattCharacteristic characteristic = (
            await (await connection.DiscoverServiceAsync(0x180F, token)).DiscoverCharacteristicsAsync(token)
        )[0];
        return new HciHostPeer(controller, peripheral, adapter, time, connection, characteristic, value);
    }

    public FakeTimeProvider Time { get; }

    public ConnectedPeripheral Connection { get; }

    public RemoteGattCharacteristic Characteristic { get; }

    public bool HoldAnswers
    {
        get => _peripheral.HoldAnswers;
        set => _peripheral.HoldAnswers = value;
    }

    public IReadOnlyList<string> Requests => [.. _peripheral.Received.Select(Describe).OfType<string>()];

    // In the controller's record, which keeps the host's packets and the peripheral's in the order they passed
    public IReadOnlyList<string> Traffic => [.. _controller.Messages.Select(DescribeTraffic).OfType<string>()];

    public Task WaitForRequestsAsync(int count) =>
        _peripheral.WaitForReceivedAsync(
            received => received.Select(Describe).OfType<string>().Count() >= count,
            $"{count} requests"
        );

    public Task ReleaseAnswerAsync() => _peripheral.ReleaseAsync();

    public void SetValue(byte[] value) => _peripheral.SetValue(_value, value);

    public Task DisconnectAsync() => _peripheral.DisconnectAsync();

    private string? DescribeTraffic(HciMessage message)
    {
        if (message.Type is not HciPacketType.HciAclData)
            return null;
        // The ATT PDU follows the ACL header and the L2CAP header, 4 bytes each
        byte[] pdu = message.PduBytes[8..];
        if (message.Direction is HciDirection.HostToController)
            return Describe(pdu);
        return (AttOpCode)pdu[0] is AttOpCode.ATT_READ_RSP or AttOpCode.ATT_WRITE_RSP ? "answer" : null;
    }

    private string? Describe(byte[] pdu) =>
        (AttOpCode)pdu[0] switch
        {
            AttOpCode.ATT_READ_REQ when pdu[1] == _value => "read",
            AttOpCode.ATT_WRITE_REQ when pdu[1] == _value => $"write {Convert.ToHexString(pdu, 3, pdu.Length - 3)}",
            _ => null,
        };

    public async ValueTask DisposeAsync()
    {
        await _adapter.DisposeAsync();
        await ((IAsyncDisposable)_controller).DisposeAsync();
    }
}
