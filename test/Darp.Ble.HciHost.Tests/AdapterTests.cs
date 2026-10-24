using Darp.Ble.Data;
using Darp.Ble.Hci.Package;
using Darp.Ble.HciHost.Verify;
using Microsoft.Extensions.Time.Testing;
using Shouldly;

namespace Darp.Ble.HciHost.Tests;

/// <summary> Opening, configuring and closing an HCI adapter </summary>
public sealed class AdapterTests
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Fact]
    public async Task OpeningAnAdapter_SetsTheControllerUpAndItsRandomAddress()
    {
        await using var controller = new FakeController();
        IReadOnlyList<BleAdapterInfo> adapters = await HciAdapter.Manager(controller).EnumerateAdaptersAsync(Token);

        await using BleAdapter adapter = await adapters.ShouldHaveSingleItem().OpenAsync(Token);

        adapters[0].Name.ShouldBe("Fake controller");
        adapters[0].Backend.ShouldBe(BleBackends.HciHost);
        adapter.RandomAddress.ShouldBe(HciAdapter.Address);
        await Verifier.Verify(controller.Messages);
    }

    [Fact]
    public async Task ASerialPortTheUserMayNotOpen_FailsAsMissingPermission_ThatWaitingDoesNotFix()
    {
        Assert.SkipWhen(Environment.IsPrivilegedProcess, "A privileged process may open any port");
        if (OperatingSystem.IsWindows())
        {
            Assert.Skip("Windows does not deny a serial port by file permissions");
            return;
        }
        string port = Path.GetTempFileName();
        File.SetUnixFileMode(port, UnixFileMode.None);
        try
        {
            var manager = new BleManager(builder => builder.AddSerialHciHost(port));
            BleAdapterInfo adapter = (await manager.EnumerateAdaptersAsync(Token)).ShouldHaveSingleItem();

            BleOpenException exception = await Should.ThrowAsync<BleOpenException>(() => adapter.OpenAsync(Token));

            exception.Kind.ShouldBe(BleOpenErrorKind.MissingPermission);
            exception.IsTransient.ShouldBeFalse();
        }
        finally
        {
            File.Delete(port);
        }
    }

    [Fact]
    public async Task ASerialPortThatIsNotThere_FailsAsNotAvailable()
    {
        var manager = new BleManager(builder => builder.AddSerialHciHost("/dev/ttyThatIsNotThere"));
        BleAdapterInfo adapter = (await manager.EnumerateAdaptersAsync(Token)).ShouldHaveSingleItem();

        BleOpenException exception = await Should.ThrowAsync<BleOpenException>(() => adapter.OpenAsync(Token));

        exception.Kind.ShouldBe(BleOpenErrorKind.NotAvailable);
    }

    [Fact]
    public async Task SettingTheRandomAddress_SendsItToTheController()
    {
        await using var controller = new FakeController();
        await using BleAdapter adapter = await HciAdapter.OpenAsync(controller);
        var address = BleAddress.CreateRandomAddress((UInt48)0xC1C2C3C4C5C6);

        await adapter.SetRandomAddressAsync(address, Token);

        adapter.RandomAddress.ShouldBe(address);
        await Verifier.Verify(controller.Messages);
    }

    [Fact]
    public async Task ACommandTheTransportRefuses_DoesNotBlockTheNextCommand()
    {
        await using var controller = new FakeController();
        await using BleAdapter adapter = await HciAdapter.OpenAsync(controller);
        var address = BleAddress.CreateRandomAddress((UInt48)0xC1C2C3C4C5C6);
        controller.Answer(HciOpCode.HCI_LE_Set_Random_Address, _ => throw new IOException("The port is busy"));
        await Should.ThrowAsync<IOException>(() => adapter.SetRandomAddressAsync(address, Token));
        controller.AnswerCommandComplete(HciOpCode.HCI_LE_Set_Random_Address, "01052000");

        await adapter.SetRandomAddressAsync(address, Token);

        adapter.RandomAddress.ShouldBe(address);
    }

    [Fact]
    public async Task AControllerThatDoesNotAnswer_FailsOpeningAfterTheCommandTimeout_OnTheManagersClock()
    {
        await using var controller = new FakeController();
        controller.Hold(HciOpCode.HCI_Reset);
        var time = new FakeTimeProvider();
        IReadOnlyList<BleAdapterInfo> adapters = await HciAdapter
            .Manager(controller, time)
            .EnumerateAdaptersAsync(Token);

        Task<BleAdapter> opening = adapters[0].OpenAsync(Token);
        time.Advance(TimeSpan.FromSeconds(4.9));
        await Task.Delay(50, Token);
        opening.IsCompleted.ShouldBeFalse();
        time.Advance(TimeSpan.FromSeconds(0.1));

        BleOpenException exception = await Should.ThrowAsync<BleOpenException>(opening);
        exception.Kind.ShouldBe(BleOpenErrorKind.NotAvailable);
        exception.IsTransient.ShouldBeTrue();
        exception.InnerException.ShouldBeOfType<TimeoutException>();
    }
}
