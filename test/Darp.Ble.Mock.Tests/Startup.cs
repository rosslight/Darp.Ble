using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit.DependencyInjection.Logging;

namespace Darp.Ble.Mock.Tests;

public sealed class Startup
{
    public void ConfigureServices(IServiceCollection services) => services
        .AddLogging(lb => lb.AddXunitOutput().SetMinimumLevel(LogLevel.Trace));
}