using Darp.Ble.Gap;

namespace Darp.Ble.Linq;

/// <summary> Extensions for handling advertising reports </summary>
public static class AdvertisementExtensions
{
    /// <summary> Attach user data to an advertisement </summary>
    /// <param name="advertisement"> The advertisement report </param>
    /// <param name="userData"> The user data to attach </param>
    /// <typeparam name="TUserData"> The type of the user data </typeparam>
    /// <returns> The advertisement with attached data</returns>
    public static IGapAdvertisement<TUserData> WithUserData<TUserData>(
        this IGapAdvertisement advertisement,
        TUserData userData
    )
    {
        return new GapAdvertisement<TUserData>(advertisement, userData);
    }
}
