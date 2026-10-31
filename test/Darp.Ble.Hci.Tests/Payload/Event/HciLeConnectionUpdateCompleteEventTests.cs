using Darp.Ble.Hci.Payload;
using Darp.Ble.Hci.Payload.Event;
using Shouldly;

namespace Darp.Ble.Hci.Tests.Payload.Event;

public sealed class HciLeConnectionUpdateCompleteEventTests
{
    [Fact]
    public void TryReadLittleEndian_AnEventFromAController_ReadsTheFieldsAfterTheStatus()
    {
        // Received from an nRF52840 controller: handle 15, interval 12 (15 ms), latency 0, timeout 960 (9.6 s)
        byte[] bytes = Convert.FromHexString("03000F000C000000C003");

        HciLeConnectionUpdateCompleteEvent
            .TryReadLittleEndian(bytes, out HciLeConnectionUpdateCompleteEvent value)
            .ShouldBeTrue();

        value.ShouldBe(
            new HciLeConnectionUpdateCompleteEvent
            {
                SubEventCode = HciLeMetaSubEventType.HCI_LE_Connection_Update_Complete,
                Status = HciCommandStatus.Success,
                ConnectionHandle = 15,
                ConnectionInterval = 12,
                PeripheralLatency = 0,
                SupervisionTimeout = 960,
            }
        );
    }
}
