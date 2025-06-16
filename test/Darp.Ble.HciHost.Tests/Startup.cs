using System.Runtime.CompilerServices;
using Darp.Ble.HciHost.Verify;
using VerifyTests.DiffPlex;

namespace Darp.Ble.HciHost.Tests;

// Magic class "Startup" to set up DI
// See https://github.com/pengweiqhca/Xunit.DependencyInjection?tab=readme-ov-file#how-to-find-startup
public sealed class Startup
{
    [ModuleInitializer]
    public static void Initialize() => VerifyDiffPlex.Initialize(OutputType.Compact);

    [ModuleInitializer]
    public static void OtherInitialize()
    {
        VerifierSettings.InitializePlugins();
        VerifierSettings.ScrubLinesContaining(StringComparison.InvariantCulture, "DiffEngineTray");
        VerifierSettings.IgnoreStackTrace();
        VerifyHciHost.Initialize();
    }
}
