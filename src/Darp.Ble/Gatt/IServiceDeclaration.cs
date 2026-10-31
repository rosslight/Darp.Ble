namespace Darp.Ble.Gatt;

/// <summary> The gatt service declaration </summary>
public interface IServiceDeclaration : IGattDeclaration
{
    /// <summary> True, if service is a primary service; False, if service is a secondary service </summary>
    GattServiceType Type { get; }
}
