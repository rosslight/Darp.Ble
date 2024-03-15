using Darp.Ble.Implementation;

namespace Darp.Ble;

public enum InitializeResult
{
    Success = 0,
    AlreadyInitializing,
}

/// <summary> The base interface of a ble device. </summary>
public sealed class BleDevice
{
    private readonly IBleDeviceImplementation _bleDevice;
    private BleObserver? _bleObserver;
    private bool _isInitializing;

    public BleDevice(IBleDeviceImplementation bleDevice)
    {
        _bleDevice = bleDevice;
    }

    public bool IsInitialized { get; private set; }

    public async Task<InitializeResult> InitializeAsync()
    {
        if (_isInitializing) return InitializeResult.AlreadyInitializing;
        try
        {
            _isInitializing = true;
            InitializeResult result = await _bleDevice.InitializeAsync();
            if (result is not InitializeResult.Success)
                return result;
            _bleObserver = new BleObserver(_bleDevice.Observer);
            IsInitialized = true;
            return InitializeResult.Success;
        }
        finally
        {
            _isInitializing = false;
        }
    }

    public Capabilities Capabilities => Capabilities.Unknown
                                        | (_bleObserver is not null ? Capabilities.Observer : Capabilities.Unknown);

    public BleObserver Observer => _bleObserver ?? throw new NotSupportedException();
}

