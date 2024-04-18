# Darp.Ble

[![.NET](https://github.com/rosslight/Darp.Ble/actions/workflows/test_and_publish.yml/badge.svg)](https://github.com/rosslight/Darp.Ble/actions/workflows/test_and_publish.yml)
[![NuGet](https://img.shields.io/nuget/v/Darp.Ble.svg)](https://www.nuget.org/packages/Darp.Ble/)
[![Downloads](https://img.shields.io/nuget/dt/Darp.Ble)](https://www.nuget.org/packages/Darp.Ble)
![License](https://img.shields.io/badge/license-AGPL--3.0%20or%20COMMERCIAL-blue)]

Darp.Ble is an abstraction layer which aims to provide a simple, reactive way of using BLE with .Net while maintaining granular configuration options.

## Disclaimer

This package is under heavy development. Huge changes to the API are to be expected!

## Implementations

| Implementation   | Observer | Central | Broadcaster | Peripheral | Nuget                                                                                                                        |
|------------------|----------|---------|-------------|------------|------------------------------------------------------------------------------------------------------------------------------|
| Darp.Ble.WinRT   | X        | X       | X           | X          | [![Darp.Ble.WinRT](https://img.shields.io/nuget/v/Darp.Ble.WinRT.svg)](https://www.nuget.org/packages/Darp.Ble.WinRT/)       |
| Darp.Ble.Android | X        | X       |             |            | [![Darp.Ble.Android](https://img.shields.io/nuget/v/Darp.Ble.Android.svg)](https://www.nuget.org/packages/Darp.Ble.Android/) |
| Darp.Ble.HciHost | X        |         |             |            | [![Darp.Ble.HciHost](https://img.shields.io/nuget/v/Darp.Ble.HciHost.svg)](https://www.nuget.org/packages/Darp.Ble.HciHost/) |
| Darp.Ble.iOS     |          |         |             |            | planned                                                                                                                      |
| Darp.Ble.Mac     |          |         |             |            | planned                                                                                                                      |
| Darp.Ble.BlueZ   |          |         |             |            | planned                                                                                                                      |
| Darp.Ble.Mock    | X        | X       |             |            | [![Darp.Ble.Mock](https://img.shields.io/nuget/v/Darp.Ble.Mock.svg)](https://www.nuget.org/packages/Darp.Ble.Mock/)          |
