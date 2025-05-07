using Argon;

namespace Darp.Ble.HciHost.Verify;

public static class VerifyHciHost
{
    public static bool Initialized { get; private set; }

    public static void Initialize()
    {
        if (Initialized)
        {
            throw new Exception("Already Initialized");
        }

        Initialized = true;
        VerifierSettings.AddExtraSettings(serializer =>
        {
            List<JsonConverter> converters = serializer.Converters;
            converters.Insert(0, new HciMessageConverter());
        });
    }
}
