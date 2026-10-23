using Darp.Ble.Data;
using Darp.Ble.HciHost.Verify;
using Microsoft.Extensions.Time.Testing;

namespace Darp.Ble.HciHost.Tests;

/// <summary> Opens an HCI adapter over a fake controller, through the public API </summary>
internal static class HciAdapter
{
    public static readonly BleAddress Address = BleAddress.CreateRandomAddress((UInt48)0xE0C5AA968B6E);

    public static BleManager Manager(FakeController controller, TimeProvider? time = null) =>
        new(
            builder =>
                builder.AddHciHost(
                    controller,
                    options =>
                    {
                        options.Name = "Fake controller";
                        options.RandomAddress = Address;
                    }
                ),
            timeProvider: time ?? new FakeTimeProvider()
        );

    public static async Task<BleAdapter> OpenAsync(FakeController controller, TimeProvider? time = null)
    {
        IReadOnlyList<BleAdapterInfo> adapters = await Manager(controller, time)
            .EnumerateAdaptersAsync(TestContext.Current.CancellationToken);
        BleAdapter adapter = await adapters[0].OpenAsync(TestContext.Current.CancellationToken);
        controller.StartRecording();
        return adapter;
    }
}
