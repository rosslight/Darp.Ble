using Darp.Ble.Device;
using Darp.Ble.Implementation;

namespace Darp.Ble;

/// <summary> The base interface of a ble device. </summary>
public sealed class BleDevice
{
    private readonly IBleDeviceImplementation _bleDevice;
    private BleObserver? _bleObserver;
    private bool _isInitializing;

    internal BleDevice(IBleDeviceImplementation bleDevice)
    {
        _bleDevice = bleDevice;
    }

    /// <summary> True if the device was successfully initialized </summary>
    public bool IsInitialized { get; private set; }

    /// <summary> Initializes the ble device </summary>
    /// <returns> Success or a custom error code </returns>
    public async Task<InitializeResult> InitializeAsync()
    {
        if (_isInitializing) return InitializeResult.AlreadyInitializing;
        try
        {
            _isInitializing = true;
            InitializeResult result = await _bleDevice.InitializeAsync();
            if (result is not InitializeResult.Success)
                return result;
            if (_bleDevice.Observer is not null)
                _bleObserver = new BleObserver(_bleDevice.Observer);
            IsInitialized = true;
            return InitializeResult.Success;
        }
        finally
        {
            _isInitializing = false;
        }
    }

    /// <summary>
    /// Gives back capabilities of this device. Before the device was successfully initialized, the capabilities are unknown
    /// </summary>
    public Capabilities Capabilities => Capabilities.Unknown
                                        | (_bleObserver is not null ? Capabilities.Observer : Capabilities.Unknown);

    /// <summary>
    /// 
    /// </summary>
    /// <exception cref="NotSupportedException"></exception>
    public BleObserver Observer => _bleObserver ?? throw new NotSupportedException();
}

