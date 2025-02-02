namespace Darp.Ble.Gap;

/// <summary> An advertising report with attached user data without known type </summary>
public interface IGapAdvertisementWithUserData : IGapAdvertisement
{
    /// <summary> The data specified by the user and attached to the advertisement </summary>
    object? UserData { get; }
}
