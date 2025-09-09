using Darp.Ble.Hci.Package;
using Darp.Ble.Hci.Payload;
using Darp.Ble.Hci.Payload.Result;

namespace Darp.Ble.HciHost.Verify;

public static class HciMessages
{
    public static HciMessage HciLeReadNumberOfSupportedAdvertisingSetsEvent(
        HciCommandStatus status = HciCommandStatus.Success,
        byte numSupportedAdvertisingSets = 1
    ) =>
        HciMessage.CommandCompleteEventToHost(
            HciOpCode.HCI_LE_Read_Number_Of_Supported_Advertising_Sets,
            new HciLeReadNumberOfSupportedAdvertisingSetsResult
            {
                Status = status,
                NumSupportedAdvertisingSets = numSupportedAdvertisingSets,
            }
        );

    public static HciMessage HciLeSetExtendedAdvertisingParametersEvent(
        HciCommandStatus status = HciCommandStatus.Success,
        sbyte selectedTxPower = 0
    ) =>
        HciMessage.CommandCompleteEventToHost(
            HciOpCode.HCI_LE_SET_EXTENDED_ADVERTISING_PARAMETERS_V1,
            new HciLeSetExtendedAdvertisingParametersResult { Status = status, SelectedTxPower = selectedTxPower }
        );

    public static HciMessage HciLeSetAdvertisingSetRandomAddressEvent(
        HciCommandStatus status = HciCommandStatus.Success
    ) =>
        HciMessage.CommandCompleteEventToHost(
            HciOpCode.HCI_LE_SET_ADVERTISING_SET_RANDOM_ADDRESS,
            new HciLeSetAdvertisingSetRandomAddressResult { Status = status }
        );

    public static HciMessage HciLeSetExtendedAdvertisingEnableEvent(
        HciCommandStatus status = HciCommandStatus.Success
    ) =>
        HciMessage.CommandCompleteEventToHost(
            HciOpCode.HCI_LE_SET_EXTENDED_ADVERTISING_ENABLE,
            new HciLeSetExtendedAdvertisingEnableResult { Status = status }
        );

    public static HciMessage HciLeRemoveAdvertisingSetEvent(HciCommandStatus status = HciCommandStatus.Success) =>
        HciMessage.CommandCompleteEventToHost(
            HciOpCode.HCI_LE_Remove_Advertising_Set,
            new HciLeRemoveAdvertisingSetResult { Status = status }
        );
}
