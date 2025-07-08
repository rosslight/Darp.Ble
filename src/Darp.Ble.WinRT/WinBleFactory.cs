using System.Runtime.Versioning;
#if !WINDOWS
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
#endif

namespace Darp.Ble.WinRT;

/// <summary> Search for the default windows ble device </summary>
/// <remarks> If used in an environment which is not build for windows, this becomes a NoOp </remarks>
[SupportedOSPlatform("windows")]
public sealed partial class WinBleFactory : IBleFactory
{
    /// <summary> The name of the resulting device </summary>
    public string Name { get; set; } = "Windows";

    /// <inheritdoc />
    public IEnumerable<IBleDevice> EnumerateDevices(IServiceProvider serviceProvider)
    {
#if WINDOWS
        yield return new WinBleDevice(serviceProvider, Name);
#else
        var logger = serviceProvider.GetService<ILogger<WinBleFactory>>();
        if (logger is not null)
            LogNoWindowsUnintentionalUse(logger, nameof(WinBleFactory), GetTargetFrameworkMoniker());
        yield break;
#endif
    }

#if !WINDOWS
    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Using the {FactoryName} while not compiled for Windows is probably unintended! You compiled for {TargetFrameworkName}"
    )]
    private static partial void LogNoWindowsUnintentionalUse(
        ILogger logger,
        string factoryName,
        string? targetFrameworkName
    );

    private static string? GetTargetFrameworkMoniker()
    {
        Assembly assembly = typeof(WinBleFactory).Assembly;
        AssemblyMetadataAttribute? meta = assembly
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .FirstOrDefault(a => a.Key.Equals("TargetFramework", StringComparison.Ordinal));
        return meta?.Value;
    }
#endif
}
