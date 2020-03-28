using System;
using Android.OS;
using Android.Systems;
using Sentry;

namespace Sentry.Xamarin
{
    public static class SentryXamarin
    {
        public static void Init(Action<XamarinOptions> configureOptions)
        {
            var options = new XamarinOptions();
            configureOptions?.Invoke(options);
            SentrySdk.Init(options);

            // TODO: This should be part of a package: Sentry.Xamarin.Android
            SentrySdk.ConfigureScope(s =>
            {
#if !ANDROID
#pragma warning disable CS0618 // Type or member is obsolete
                var friendlyName = $"Android:{Build.Manufacturer}-{Build.CpuAbi}-{Build.Model}";
                StructUtsname uname = null;
                if (Build.VERSION.SdkInt > BuildVersionCodes.Lollipop)
                {
                    try
                    {
                        uname = Os.Uname();
                        friendlyName += $"-kernel-{uname.Release}";
                        s.Contexts["uname"] = new
                        {
                            uname.Machine,
                            uname.Nodename,
                            uname.Release,
                            uname.Sysname,
                            uname.Version
                        };
                    }
                    catch
                    {
                        // android.runtime.JavaProxyThrowable: System.NotSupportedException: Could not activate JNI Handle 0x7ed00025 (key_handle 0x4192edf8) of Java type 'md5eb7159ad9d3514ee216d1abd14b6d16a/MainActivity' as managed type 'SymbolCollector.Android.MainActivity'. --->
                        // Java.Lang.NoClassDefFoundError: android/system/Os ---> Java.Lang.ClassNotFoundException: Didn't find class "android.system.Os" on path: DexPathList[[zip file "/data/app/SymbolCollector.Android.SymbolCollector.Android-1.apk"],nativeLibraryDirectories=[/data/app-lib/SymbolCollector.Android.SymbolCollector.Android-1, /vendor/lib, /system/lib]]
                    }
                }

                s.User.Id = Build.Id;
                s.Contexts.Device.Architecture = Build.CpuAbi;
                s.Contexts.Device.Brand = Build.Brand;
                s.Contexts.Device.Manufacturer = Build.Manufacturer;
                s.Contexts.Device.Model = Build.Model;

                s.Contexts.OperatingSystem.Name = "Android";
                s.Contexts.OperatingSystem.KernelVersion = uname?.Release;
                s.Contexts.OperatingSystem.Version = Build.VERSION.SdkInt.ToString();

                s.SetTag("API", ((int)Build.VERSION.SdkInt).ToString());
                s.SetTag("host", Build.Host);
                s.SetTag("device", Build.Device);
                s.SetTag("product", Build.Product);
                s.SetTag("cpu-abi", Build.CpuAbi);
                s.SetTag("fingerprint", Build.Fingerprint);

                if (!string.IsNullOrEmpty(Build.CpuAbi2))
                {
                    s.SetTag("cpu-abi2", Build.CpuAbi2);
                }
#pragma warning restore CS0618 // Type or member is obsolete
#endif
            });
        }
    }

    public class XamarinOptions : SentryOptions
    {
        public XamarinOptions()
        {
            this.AddInAppExclude("Mono");
        }
    }
}
