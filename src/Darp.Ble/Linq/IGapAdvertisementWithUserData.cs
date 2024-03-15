using Darp.Ble.Gap;

namespace Darp.Ble.Linq;

public interface IGapAdvertisementWithUserData : IGapAdvertisement
{
    /// <summary> The data specified by the user and attached to the advertisement </summary>
    object? UserData { get; }
}