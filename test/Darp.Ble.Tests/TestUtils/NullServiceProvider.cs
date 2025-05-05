namespace Darp.Ble.Tests.TestUtils;

internal sealed class NullServiceProvider : IServiceProvider
{
    public static readonly IServiceProvider Instance = new NullServiceProvider();

    public object? GetService(Type serviceType) => null;
}
