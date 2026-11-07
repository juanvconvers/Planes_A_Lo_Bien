using UnityEngine;

namespace Tesela.Services
{
    /// <summary>Vibración breve con VibrationEffect (Android 8+) por JNI; reemplazo mínimo propio de librerías de háptica.</summary>
    public static class Haptics
    {
        public static bool Enabled = true;

#if UNITY_ANDROID && !UNITY_EDITOR
        private static AndroidJavaObject _vibrator;
        private static int _sdk;

        private static AndroidJavaObject Vibrator
        {
            get
            {
                if (_vibrator != null) return _vibrator;
                using (var version = new AndroidJavaClass("android.os.Build$VERSION")) _sdk = version.GetStatic<int>("SDK_INT");
                using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (var activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
                    _vibrator = activity.Call<AndroidJavaObject>("getSystemService", "vibrator");
                return _vibrator;
            }
        }
#endif

        public static void Light() => Pulse(12, 70);
        public static void Medium() => Pulse(22, 140);
        public static void Strong() => Pulse(40, 220);

        public static void Pulse(long milliseconds, int amplitude)
        {
            if (!Enabled) return;
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                var v = Vibrator;
                if (v == null || !v.Call<bool>("hasVibrator")) return;
                if (_sdk >= 26)
                {
                    using (var effectClass = new AndroidJavaClass("android.os.VibrationEffect"))
                    using (var effect = effectClass.CallStatic<AndroidJavaObject>("createOneShot", milliseconds, Mathf.Clamp(amplitude, 1, 255)))
                        v.Call("vibrate", effect);
                }
                // Handheld.Vibrate también hace que Unity declare el permiso VIBRATE en el manifiesto.
                else Handheld.Vibrate();
            }
            catch (AndroidJavaException) { }
#endif
        }
    }
}
