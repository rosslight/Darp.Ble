using System.Reactive.Linq;
using Darp.Ble.Data;
using Darp.Ble.Gap;
using Darp.Ble.Implementation;
using Serilog.Events;

namespace Darp.Ble.Examples.Unix.Mockup;

internal class BMDevice(string name, IObserver<(BleDevice, LogEvent)>? logger) : BleDevice(logger)
{
    public override string Identifier => "Darp.Ble.Mock";

    public override string? Name { get; } = name;

    protected override Task<InitializeResult> InitializeAsyncCore(CancellationToken cancellationToken)
    {
        var broadcaster = new BMBroadcaster();
        Observer = new BMObserver(this, broadcaster, Logger);

        broadcaster.Advertise(Observable.Empty<AdvertisingData>(), new AdvertisingParameters
        {
            Type = BleEventType.None,
        });

        return Task.FromResult(InitializeResult.Success);
    }
}