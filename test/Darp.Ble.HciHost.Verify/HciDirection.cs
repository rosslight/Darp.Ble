namespace Darp.Ble.HciHost.Verify;

/// <summary> The direction an <see cref="HciMessage"/> is sent </summary>
public enum HciDirection
{
    /// <summary> The <see cref="HciMessage"/> was sent from the host to the controller </summary>
    HostToController,

    /// <summary> The <see cref="HciMessage"/> was sent from the controller to the host </summary>
    ControllerToHost,
}
