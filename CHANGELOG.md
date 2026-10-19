# Changelog

## [2.0.0-preview](https://github.com/rosslight/Darp.Ble/compare/v1.10.2...v2.0.0-preview) (2026-10-19)


### ⚠ BREAKING CHANGES

* BleAddressType.RandomStatic, RandomPrivateResolvable, RandomPrivateNonResolvable and ResolvablePrivateAddress are replaced by Random, PublicIdentity, RandomStaticIdentity and RandomUnresolved. BleAddress.CreateRandomAddress always returns the type Random.

### Features

* bump to net 10 ([#124](https://github.com/rosslight/Darp.Ble/issues/124)) ([74445b3](https://github.com/rosslight/Darp.Ble/commit/74445b3ba66cd0e831f72a0b41868d9a9abfe48c))


### Bug Fixes

* Convert UInt48 to ulong without reading past the struct ([#150](https://github.com/rosslight/Darp.Ble/issues/150)) ([0c954fb](https://github.com/rosslight/Darp.Ble/commit/0c954fb3018c9446255ff7416708101538f9c86a))
* Handle truncated and long advertising data sections ([#146](https://github.com/rosslight/Darp.Ble/issues/146)) ([2e82a7c](https://github.com/rosslight/Darp.Ble/commit/2e82a7ced0afdba3b6754ab753d804b396133793))
* Reject a zero length in ATT_READ_BY_GROUP_TYPE_RSP ([#151](https://github.com/rosslight/Darp.Ble/issues/151)) ([37db297](https://github.com/rosslight/Darp.Ble/commit/37db29731936f40662f5660008e5ea26d6b44ba9))
* Reject truncated extended advertising report events ([#147](https://github.com/rosslight/Darp.Ble/issues/147)) ([bd90f47](https://github.com/rosslight/Darp.Ble/commit/bd90f477731c949c647c1ee62963e5d12e9564ea))
* Report valid event types for advertisements on Windows ([#148](https://github.com/rosslight/Darp.Ble/issues/148)) ([7006ae0](https://github.com/rosslight/Darp.Ble/commit/7006ae06bd5c46ef8012a46abdc8baaa02d3a70d))
* Use the HCI address types for BleAddressType ([#149](https://github.com/rosslight/Darp.Ble/issues/149)) ([36d7792](https://github.com/rosslight/Darp.Ble/commit/36d7792108582d3c610003d0e09c86da23d1dfb0))

## [1.10.2](https://github.com/rosslight/Darp.Ble/compare/v1.10.1...v1.10.2) (2026-04-16)


### Bug Fixes

* Properly propagate transport errors ([#110](https://github.com/rosslight/Darp.Ble/issues/110)) ([bbc293b](https://github.com/rosslight/Darp.Ble/commit/bbc293b65df6a91fdb996a9b8d773b9a9a5c2760))
* Throw targeted exceptions if disconnection event occurs ([#108](https://github.com/rosslight/Darp.Ble/issues/108)) ([deb60ca](https://github.com/rosslight/Darp.Ble/commit/deb60ca22568661dafb107875b7e522356f63f71))

## [1.10.1](https://github.com/rosslight/Darp.Ble/compare/v1.10.0...v1.10.1) (2026-01-20)


### Bug Fixes

* Dispose of the crypto transform ([#99](https://github.com/rosslight/Darp.Ble/issues/99)) ([b1f9acf](https://github.com/rosslight/Darp.Ble/commit/b1f9acfa1241528e292a22a0f01f5af1fc9b93b1))
* race conditions in ble observer ([#98](https://github.com/rosslight/Darp.Ble/issues/98)) ([97bd796](https://github.com/rosslight/Darp.Ble/commit/97bd79629826ea2ba4de47886cb5ab24aaef426f))
