using Darp.Ble.Data;
using Darp.Ble.Implementation;
using Darp.Ble.Logger;

namespace Darp.Ble;

/// <summary> The base interface of a ble device. </summary>
public sealed class BleDevice
{
    private readonly IPlatformSpecificBleDevice _platformSpecificBleDevice;
    private readonly IObserver<LogEvent>? _logger;
    private bool _isInitializing;
    private BleObserver? _bleObserver;
    private BleCentral? _bleCentral;

    internal BleDevice(IPlatformSpecificBleDevice platformSpecificBleDevice, IObserver<(BleDevice, LogEvent)>? logger)
    {
        _platformSpecificBleDevice = platformSpecificBleDevice;
        if (logger is not null) _logger = System.Reactive.Observer.Create<LogEvent>(x => logger.OnNext((this, x)));
    }

    /// <summary> True if the device was successfully initialized </summary>
    public bool IsInitialized { get; private set; }

    /// <summary> Get an implementation specific identification string </summary>
    public string Identifier => _platformSpecificBleDevice.Identifier;

    /// <summary> Initializes the ble device </summary>
    /// <returns> Success or a custom error code </returns>
    public async Task<InitializeResult> InitializeAsync()
    {
        if (_isInitializing) return InitializeResult.AlreadyInitializing;
        try
        {
            _isInitializing = true;
            InitializeResult result = await _platformSpecificBleDevice.InitializeAsync();
            if (result is not InitializeResult.Success)
                return result;
            if (_platformSpecificBleDevice.Observer is not null)
                _bleObserver = new BleObserver(this, _platformSpecificBleDevice.Observer, _logger);
            //if (_platformSpecificBleDevice.Central is not null)
            //    _bleCentral = new BleCentral(this, _platformSpecificBleDevice.Central, _logger);
            IsInitialized = true;
            _logger?.Debug("Adapter Initialized!");
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
    public Capabilities Capabilities => Capabilities.None
                                        | (_bleObserver is not null ? Capabilities.Observer : Capabilities.None);

    /// <summary> Returns a view of the device in Observer Role </summary>
    /// <exception cref="NotSupportedException"> Thrown when the device has not been initialized or the role is not supported </exception>
    public BleObserver Observer => _bleObserver ?? throw new NotSupportedException();
    /// <summary> Returns a view of the device in Central Role </summary>
    /// <exception cref="NotSupportedException"> Thrown when the device has not been initialized or the role is not supported </exception>
    public BleCentral Central => _bleCentral ?? throw new NotSupportedException();
}

