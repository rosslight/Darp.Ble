using Microsoft.Extensions.DependencyInjection;
using Xunit.DependencyInjection.Logging;

namespace Darp.Ble.Tests;

public sealed class Startup
{
    public void ConfigureServices(IServiceCollection services) =>
        services.AddLogging(lb => lb.AddXunitOutput());
}
