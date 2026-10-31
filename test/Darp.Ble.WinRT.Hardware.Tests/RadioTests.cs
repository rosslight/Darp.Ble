using Darp.Ble.Data;
using Darp.Ble.Data.AssignedNumbers;
using Darp.Ble.Gap;
using Darp.Ble.Gatt;
using Shouldly;
using Windows.Devices.Radios;

namespace Darp.Ble.WinRT.Hardware.Tests;

/// <summary>
/// Windows' adapter while its Bluetooth radio turns off. The tests switch the machine's radio, which drops its other
/// Bluetooth devices, so they only run when asked for (<c>--explicit only</c>); they need no dongle
/// </summary>
[Collection(nameof(HardwareTests))]
public sealed class RadioTests
{
    private static readonly TimeSpan Patience = TimeSpan.FromSeconds(20);

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private static async Task<BleAdapter> OpenWindowsAsync()
    {
        IReadOnlyList<BleAdapterInfo> adapters = await new BleManager(builder =>
            builder.AddWinRT()
        ).EnumerateAdaptersAsync(Token);
        return await adapters.ShouldHaveSingleItem().OpenAsync(Token);
    }

    private static async Task ScanToTheEndAsync(BleAdapter adapter)
    {
        await foreach (IGapAdvertisement _ in adapter.Observer.ScanAsync(Token)) { }
    }

    [Fact(Explicit = true)]
    public async Task Opening_WhileTheRadioIsOff_FailsAsDisabled_AndWorksOnceItIsOn()
    {
        await using RadioSwitch radio = await RadioSwitch.GetAsync();
        await radio.SetAsync(RadioState.Off);

        BleOpenException error = await Should.ThrowAsync<BleOpenException>(OpenWindowsAsync());
        await radio.SetAsync(RadioState.On);

        error.Kind.ShouldBe(BleOpenErrorKind.Disabled);
        error.IsTransient.ShouldBeTrue();
        await using BleAdapter windows = await OpenWindowsAsync();
    }

    [Fact(Explicit = true)]
    public async Task AScan_WhenTheRadioTurnsOff_FailsAsTransient()
    {
        await using RadioSwitch radio = await RadioSwitch.GetAsync();
        await using BleAdapter windows = await OpenWindowsAsync();
        Task scan = ScanToTheEndAsync(windows);
        await Task.Delay(TimeSpan.FromSeconds(1), Token);

        await radio.SetAsync(RadioState.Off);

        BleException error = await Should.ThrowAsync<BleException>(scan.WaitAsync(Patience, Token));
        error.IsTransient.ShouldBeTrue();
    }

    [Fact(Explicit = true)]
    public async Task StartingAScan_WhileTheRadioIsOff_FailsAsTransient()
    {
        await using RadioSwitch radio = await RadioSwitch.GetAsync();
        await using BleAdapter windows = await OpenWindowsAsync();
        await radio.SetAsync(RadioState.Off);

        BleException error = await Should.ThrowAsync<BleException>(
            ScanToTheEndAsync(windows).WaitAsync(Patience, Token)
        );

        error.IsTransient.ShouldBeTrue();
    }

    [Fact(Explicit = true)]
    public async Task AnAdvertisingSet_StoppedByTheRadio_StartsAgainOnceTheRadioIsOn()
    {
        await using RadioSwitch radio = await RadioSwitch.GetAsync();
        await using BleAdapter windows = await OpenWindowsAsync();
        await using AdvertisingSet set = await windows.Broadcaster.StartAdvertisingAsync(
            AdvertisingData.From([(AdTypes.ManufacturerSpecificData, new byte[] { 0xFF, 0xFF, 0xDA, 0x7B, 1 })]),
            TimeSpan.FromMilliseconds(100),
            BleEventType.AdvNonConnInd,
            cancellationToken: Token
        );

        await radio.SetAsync(RadioState.Off);
        // The set knows it stopped, so starting it really starts the publisher, which fails while the radio is off
        BleException whileOff = await Should.ThrowAsync<BleException>(set.StartAsync(Token).WaitAsync(Patience, Token));
        await radio.SetAsync(RadioState.On);

        whileOff.IsTransient.ShouldBeTrue();
        await StartOnceTheRadioIsBackAsync(set);
    }

    /// <summary> Windows reports the radio on about a second before it advertises again, so a transient failure is retried, as an app would </summary>
    private static async Task StartOnceTheRadioIsBackAsync(AdvertisingSet set)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(Token);
        timeout.CancelAfter(Patience);
        while (true)
        {
            try
            {
                await set.StartAsync(timeout.Token);
                return;
            }
            catch (BleException e) when (e.IsTransient)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(500), timeout.Token);
            }
        }
    }

    [Fact(Explicit = true)]
    public async Task StartingAConnectableSet_WhileTheRadioIsOff_FailsAsTransient()
    {
        await using RadioSwitch radio = await RadioSwitch.GetAsync();
        await using BleAdapter windows = await OpenWindowsAsync();
        var service = new LocalGattServiceBuilder(BleUuid.Parse("6E400001-B5A3-F393-E0A9-E50E24DCCA9E", null));
        service.AddCharacteristic(
            BleUuid.Parse("6E400002-B5A3-F393-E0A9-E50E24DCCA9E", null),
            GattProperty.Read,
            onRead: (_, _) => ValueTask.FromResult(new byte[] { 42 })
        );
        await windows.Peripheral.AddServiceAsync(service, Token);
        await radio.SetAsync(RadioState.Off);

        BleException error = await Should.ThrowAsync<BleException>(
            windows
                .Broadcaster.StartAdvertisingAsync(
                    AdvertisingData.Empty,
                    TimeSpan.FromMilliseconds(100),
                    BleEventType.AdvInd,
                    cancellationToken: Token
                )
                .WaitAsync(Patience, Token)
        );

        error.IsTransient.ShouldBeTrue();
    }

    /// <summary> Switches the Bluetooth radio within one test, and always back on at its end </summary>
    private sealed class RadioSwitch : IAsyncDisposable
    {
        private readonly Radio _radio;

        private RadioSwitch(Radio radio) => _radio = radio;

        public static async Task<RadioSwitch> GetAsync()
        {
            (await Radio.RequestAccessAsync()).ShouldBe(RadioAccessStatus.Allowed);
            IReadOnlyList<Radio> radios = await Radio.GetRadiosAsync();
            return new RadioSwitch(radios.Single(radio => radio.Kind is RadioKind.Bluetooth));
        }

        public async Task SetAsync(RadioState state)
        {
            (await _radio.SetStateAsync(state)).ShouldBe(RadioAccessStatus.Allowed);
            _radio.State.ShouldBe(state);
            // Windows reports the radio on about a second before it advertises again; the next test starts after that
            if (state is RadioState.On)
                await Task.Delay(TimeSpan.FromSeconds(2));
        }

        public async ValueTask DisposeAsync() => await SetAsync(RadioState.On);
    }
}
