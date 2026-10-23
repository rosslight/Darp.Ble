# Telemetry

Darp.Ble's logs, spans and metrics are part of its API. The names, event ids, tag keys and units below change only additively; renames and removals are breaking changes and go into the release notes. The wording of messages is not part of the contract.

Subscribe with the names in `BleTelemetry`, e.g. with OpenTelemetry:

```csharp
tracing.AddSource(BleTelemetry.SourceName);   // "Darp.Ble"
metrics.AddMeter(BleTelemetry.MeterName);     // "Darp.Ble"
```

`BleManagerBuilder.ConfigureDiagnostics(options => options.IncludeAddresses = false)` replaces peer addresses with `<hidden>` in logs and spans. Addresses are never metric dimensions.

The backends add their own sources and event ids when they are ported to v2 (HciHost, WinRT, Android); their spans nest inside the core's.

## Spans (source `Darp.Ble`)

Span names are message templates; values are only in tags. A cancelled operation has status Unset and outcome `cancelled`; any other failure has status Error.

| Span | Kind | From – to | Tags |
| --- | --- | --- | --- |
| `Open adapter {Backend}` | Internal | the backend opens the adapter – the adapter is ready or opening failed | backend, outcome |
| `Scan` | Consumer | one enumeration of `ScanAsync`, from its start to its end; never one per advertisement | backend, outcome |
| `Connect` | Client | `ConnectAsync`, incl. pausing scans, the link and the MTU exchange | backend, address, outcome |
| `Exchange MTU`, `Discover services`, `Discover characteristics {Uuid}`, `Discover descriptors {Uuid}`, `Read {Uuid}`, `Write {Uuid}`, `Write without response {Uuid}`, `Read descriptor {Uuid}`, `Write descriptor {Uuid}`, `Enable notifications {Uuid}`, `Disable notifications {Uuid}` | Client | a GATT operation, from being queued to its end, also after its caller gave up; a child of the caller's activity | backend, operation, uuid, address, outcome |
| `Handle {Operation} {Uuid}` | Server | one run of a hosted read or write handler | operation, uuid, address, outcome |

| Tag | Meaning |
| --- | --- |
| `darp.ble.backend` | the backend, e.g. `Darp.Ble.Mock` (`BleBackends`) |
| `darp.ble.operation` | `exchange_mtu`, `discover_services`, `discover_characteristics`, `discover_descriptors`, `read`, `write`, `write_without_response`, `read_descriptor`, `write_descriptor`, `enable_notifications`, `disable_notifications`; hosted: `read`, `write`, `write_command` |
| `darp.ble.uuid` | the service, characteristic or descriptor the operation is about |
| `darp.ble.address` | the peer's address, or `<hidden>` |
| `darp.ble.outcome` | `success`, `cancelled`, `att_error`, `disconnected`, `error` |
| `darp.ble.queue` | `advertisements`, `notifications` |
| `darp.ble.role` | `central` (this app connected to a peripheral), `peripheral` (a central connected to this app) |

## Metrics (meter `Darp.Ble`)

| Instrument | Type | Unit | Dimensions |
| --- | --- | --- | --- |
| `darp.ble.gatt.client.request.duration` | histogram | s | backend, operation, outcome |
| `darp.ble.queue.dropped` | counter | {item} | backend, queue |
| `darp.ble.connection.count` | up-down counter | {connection} | backend, role |

## Log events (category `Darp.Ble`)

| Id | Name | Level | Message |
| --- | --- | --- | --- |
| 1000 | AdapterOpened | Debug | Adapter {AdapterName} of backend {Backend} opened |
| 1001 | AdapterDisposed | Debug | Adapter {AdapterName} of backend {Backend} disposed |
| 1002 | AdapterOpenFailed | Warning | Adapter {AdapterName} of backend {Backend} could not be opened: {Kind} |
| 1100 | ScanStarted | Debug | Radio scan started ({ScanType}) |
| 1101 | ScanStopped | Debug | Radio scan stopped |
| 1102 | ScanReconfigured | Warning | Scan settings changed while scanning, from {Previous} to {Current}; the radio scan restarts |
| 1103 | ScanFailed | Warning | Scan failed |
| 1104 | ScanQueueOverflow | Warning | A scan ended because its queue of {Capacity} advertisements was full |
| 1200 | AdvertisingStarted | Debug | Advertising set {Address} started |
| 1201 | AdvertisingStopped | Debug | Advertising set {Address} stopped |
| 1202 | AdvertisingRestartFailed | Warning | Advertising set {Address} could not start again after its central disconnected |
| 1300 | Connecting | Debug | Connecting to {Address} |
| 1301 | Connected | Debug | Connected to {Address} |
| 1302 | ConnectFailed | Warning | Connecting to {Address} failed: {Kind} |
| 1303 | Disconnected | Debug | Connection to {Address} ended: {Reason} |
| 1304 | RequestAbandoned | Debug | A request to {Address} was abandoned after sending; the next request waits for its answer |
| 1305 | AbandonedRequestCompleted | Debug | An abandoned request to {Address} ended after {Duration} |
| 1306 | AttTimeout | Warning | {Address} did not answer within the ATT timeout; ending the connection |
| 1307 | ConnectionCleanupFailed | Warning | Releasing the connection to {Address} failed |
| 1400 | CentralConnected | Debug | Central {Address} connected |
| 1401 | CentralDisconnected | Debug | Connection of central {Address} ended: {Reason} |
| 1402 | HandlerFailed | Warning | A handler for central {Address} failed; answering with UnlikelyError |
| 1403 | NotifyAllCentralFailed | Warning | A notification to central {Address} failed; the other centrals still get theirs |
| 1404 | StateDisposeFailed | Warning | Disposing a state of central {Address} failed |
| 1405 | SlowHandlers | Warning | Handlers of central {Address} still run {Delay} after its connection started ending; its cleanup waits for them |

Advertising events show the adapter's own advertising address, which `IncludeAddresses` does not hide.
