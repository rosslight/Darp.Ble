using Darp.Ble.Data;

namespace Darp.Ble.Mock;

/// <summary> Mock device specific settings </summary>
public sealed class MockDeviceSettings
{
    /// <summary> A function to convert from tx power to rssi </summary>
    public Func<TxPowerLevel, Rssi> TxPowerToRssi { get; set; } = txPower => CalculateRssi(txPower, 2, 2);

    /// <summary>
    /// Calculates the Received Signal Strength Indicator (RSSI) based on the Log-Distance Path Loss Model. Based on: <br/>
    /// <c>Distance = 10^((Measured Power - Instant RSSI)/(10*N))</c>
    /// </summary>
    /// <param name="txPower"> The power of the sender </param>
    /// <param name="distance"> The distance between sender and receiver in meter. Distance=1m leads to the RSSI being equal to the TxPower </param>
    /// <param name="environmentalFactor"> A environmental factor. Normally between 2 and 4 </param>
    /// <returns> The received signal strength </returns>
    public static Rssi CalculateRssi(TxPowerLevel txPower, double distance, double environmentalFactor)
    {
        return (Rssi)(sbyte)((double)txPower - (10.0d * Math.Log10(distance) * environmentalFactor));
    }
}