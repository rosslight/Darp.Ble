# Examples

Each example is a single C# file that shows one task. Run one from the repository root with the .NET 10 SDK:

```sh
dotnet run examples/list-adapters.cs
```

| Example | What it shows |
| --- | --- |
| [`list-adapters.cs`](list-adapters.cs) | List the adapters of the registered backends |
| [`open-adapter.cs`](open-adapter.cs) | Open an adapter by name, print its roles, and handle a failed open |
| [`scan.cs`](scan.cs) | Scan for a few seconds and print every advertisement |
| [`advertise.cs`](advertise.cs) | Advertise a name until Ctrl+C |

The examples use the in-process mock backend, so they run without Bluetooth hardware.
