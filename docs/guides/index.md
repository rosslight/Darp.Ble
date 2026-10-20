# Guides

Guides are task flows for an initialized `IBleDevice`. Choose and set up a backend under [Platforms](../platforms/) first, then use these pages for the BLE workflow itself.

## Guide map

| Goal | Read |
| --- | --- |
| Observe nearby BLE advertisements | [Scan advertisements](scan-advertisements/) |
| Initiate a connection and use remote GATT | [Connect as a central](connect-as-central/) |
| Publish advertising data | [Broadcast advertisements](broadcast-advertisements/) |
| Host local GATT and notify connected centrals | [Accept connections](accept-connections/) |
| Test without physical BLE hardware | [Testing with mock](testing-with-mock/) |

## Before using a guide

- Call `InitializeAsync()` before using a role.
- Check `IBleDevice.Capabilities` before accessing `Observer`, `Central`, `Broadcaster`, or `Peripheral`.
- Keep platform setup details in [Platforms](../platforms/); these guides focus on the shared workflow.
