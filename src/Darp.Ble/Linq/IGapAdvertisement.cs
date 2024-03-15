namespace Darp.Ble.Linq;

public interface IGapAdvertisement<out TUserData> : IGapAdvertisementWithUserData
{
    /// <summary> The data specified by the user and attached to the advertisement </summary>
    new TUserData UserData { get; }
}