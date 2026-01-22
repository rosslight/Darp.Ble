using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using Shouldly;

namespace Darp.Ble.Tests.TestUtils;

public static class ShouldlyHelpers
{
    public static async Task ShouldPushAtLeastAsync<T>(this IObservable<T> observable, int numberOfItems)
    {
        int pushedItemsInObservable = await observable.Take(numberOfItems).Count();

        pushedItemsInObservable.ShouldBeGreaterThanOrEqualTo(numberOfItems);
    }

    public static async Task<Exception> ShouldThrowAsync<T>(this IObservable<object> observable)
        where T : Exception
    {
        return await observable.ToTask().ShouldThrowAsync<T>();
    }
}
