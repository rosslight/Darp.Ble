using Darp.Ble.Data;
using Darp.Ble.Diagnostics;

namespace Darp.Ble.Connections;

/// <summary> What a queued GATT request is, for its span and metric </summary>
/// <param name="Name"> The operation, a bounded value such as "read" </param>
/// <param name="DisplayName"> The span name, a template without values </param>
/// <param name="Uuid"> The attribute the request is about, if any </param>
internal readonly record struct GattOperation(string Name, string DisplayName, BleUuid? Uuid)
{
    public static GattOperation ExchangeMtu { get; } = new("exchange_mtu", "Exchange MTU", Uuid: null);

    public static GattOperation DiscoverServices { get; } = new("discover_services", "Discover services", Uuid: null);

    public static GattOperation DiscoverCharacteristics(BleUuid service) =>
        new("discover_characteristics", "Discover characteristics {Uuid}", service);

    public static GattOperation DiscoverDescriptors(BleUuid characteristic) =>
        new("discover_descriptors", "Discover descriptors {Uuid}", characteristic);

    public static GattOperation Read(BleUuid uuid) => new("read", "Read {Uuid}", uuid);

    public static GattOperation Write(BleUuid uuid) => new("write", "Write {Uuid}", uuid);

    public static GattOperation WriteWithoutResponse(BleUuid uuid) =>
        new("write_without_response", "Write without response {Uuid}", uuid);

    public static GattOperation ReadDescriptor(BleUuid uuid) => new("read_descriptor", "Read descriptor {Uuid}", uuid);

    public static GattOperation WriteDescriptor(BleUuid uuid) =>
        new("write_descriptor", "Write descriptor {Uuid}", uuid);

    public static GattOperation SetNotifications(BleUuid uuid, bool enabled) =>
        enabled
            ? new("enable_notifications", "Enable notifications {Uuid}", uuid)
            : new("disable_notifications", "Disable notifications {Uuid}", uuid);

    /// <summary> Starts the span of the request; nothing when nobody listens </summary>
    public System.Diagnostics.Activity? Start(string backend, object peer)
    {
        System.Diagnostics.Activity? activity = Telemetry.Source.StartActivity(
            DisplayName,
            System.Diagnostics.ActivityKind.Client
        );
        if (activity is { IsAllDataRequested: true })
        {
            activity.SetTag(Telemetry.BackendTag, backend);
            activity.SetTag(Telemetry.OperationTag, Name);
            activity.SetTag(Telemetry.AddressTag, Telemetry.Text(peer));
            if (Uuid is { } uuid)
                activity.SetTag(Telemetry.UuidTag, Telemetry.Text(uuid));
        }
        return activity;
    }
}
