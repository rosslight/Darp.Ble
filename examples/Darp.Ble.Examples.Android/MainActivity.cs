using Android.Bluetooth;
using Darp.Ble.Android;
using Exception = Java.Lang.Exception;

namespace Darp.Ble.Examples.Android;

[Activity(Label = "@string/app_name", MainLauncher = true)]
public class MainActivity : Activity
{
    private BleManager? _bleManager;
    public BleManager BleManager => _bleManager ?? throw new Exception("Not initialized yet");
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        var manager = GetSystemService(BluetoothService) as BluetoothManager;

        _bleManager = new BleManagerBuilder()
            .With(new AndroidBleFactory(manager))
            .CreateManager();

        base.OnCreate(savedInstanceState);

        // Set our view from the "main" layout resource
        //SetContentView(Resource.Layout.activity_main);
    }
}