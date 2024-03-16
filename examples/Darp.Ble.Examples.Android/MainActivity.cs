using Android.Bluetooth;
using Darp.Ble.Android;

namespace Darp.Ble.Examples.Android;

[Activity(Label = "@string/app_name", MainLauncher = true)]
public class MainActivity : Activity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        var manager = GetSystemService(BluetoothService) as BluetoothManager;

        BleManager bleManager = new BleManagerBuilder()
            .With(new AndroidBleFactory(manager))
            .CreateManager();

        base.OnCreate(savedInstanceState);

        // Set our view from the "main" layout resource
        //SetContentView(Resource.Layout.activity_main);
    }
}