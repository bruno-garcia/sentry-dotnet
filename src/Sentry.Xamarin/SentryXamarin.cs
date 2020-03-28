using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
#if __ANDROID__
using Android.OS;
using Android.Systems;
#endif
using Sentry;
using Sentry.Extensibility;
using Sentry.Protocol;

namespace Sentry.Xamarin
{
    internal class MonoStackTraceParser : ISentryStackTraceFactory
    {
        public SentryStackTrace Create(Exception exception = null)
        {
            if (exception is null)
            {
                return null;
            }

            List<StackFrameData> frames = null;
            if (exception.StackTrace is string stacktrace)
            {
                foreach (var line in stacktrace.Split('\n'))
                {
                    if (StackFrameData.TryParse(line, out var frame))
                    {
                        if (frames == null)
                        {
                            frames = new List<StackFrameData>();
                        }
                        frames.Add(frame);
                    }
                }
            }
            if (frames != null)
            {
                return new SentryStackTrace
                {
                    Frames = frames.Select(f => new SentryStackFrame
                    {
                        //string TypeFullName;
                        //string MethodSignature;
                        //int Offset;
                        //bool IsILOffset;
                        //uint MethodIndex;
                        //string Line;
                        //string Mvid;
                        //string Aotid;
                        Package = f.TypeFullName,
                        InstructionOffset = f.Offset,
                        Platform = "mono",
                        Function = f.MethodSignature,
                        LineNumber = GetLine(f.Line),
                        ContextLine = $"MVID:{f.Mvid}-AOTID:{f.Aotid}-MethodIndex:{f.MethodIndex}-IsILOffset:{f.IsILOffset}"
                    }).ToArray()
                };

                int? GetLine(string line) =>
                  line is string l && int.TryParse(l, out var parsedLine) ? (int?)parsedLine : (int?)null;
            } else
            {
                return null;
            }
        }
    }
    public static class SentryXamarin
    {
        public static void Init(Action<XamarinOptions> configureOptions)
        {
            var options = new XamarinOptions();
            options.UseStackTraceFactory(new MonoStackTraceParser());

            configureOptions?.Invoke(options);
            SentrySdk.Init(options);

            // TODO: This should be part of a package: Sentry.Xamarin.Android
            SentrySdk.ConfigureScope(s =>
            {
#if __ANDROID__
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
#elif __IOS__
                
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

    // https://github.com/mono/mono/blob/d336d6be307dfea8b7a07268270c6d885db9d399/mcs/tools/mono-symbolicate/StackFrameData.cs
    internal class StackFrameData
    {
        static Regex regex = new Regex(@"\w*at (?<Method>.+) *(\[0x(?<IL>.+)\]|<0x.+ \+ 0x(?<NativeOffset>.+)>( (?<MethodIndex>\d+)|)) in <(?<MVID>[^>#]+)(#(?<AOTID>[^>]+)|)>:0");

        public readonly string TypeFullName;
        public readonly string MethodSignature;
        public readonly int Offset;
        public readonly bool IsILOffset;
        public readonly uint MethodIndex;
        public readonly string Line;
        public readonly string Mvid;
        public readonly string Aotid;

        private StackFrameData(string line, string typeFullName, string methodSig, int offset, bool isILOffset, uint methodIndex, string mvid, string aotid)
        {
            Line = line;
            TypeFullName = typeFullName;
            MethodSignature = methodSig;
            Offset = offset;
            IsILOffset = isILOffset;
            MethodIndex = methodIndex;
            Mvid = mvid;
            Aotid = aotid;
        }

        public StackFrameData Relocate(string typeName, string methodName)
        {
            return new StackFrameData(Line, typeName, methodName, Offset, IsILOffset, MethodIndex, Mvid, Aotid);
        }

        public static bool TryParse(string line, out StackFrameData stackFrame)
        {
            stackFrame = null;

            var match = regex.Match(line);
            if (!match.Success)
                return false;

            string typeFullName, methodSignature;
            var methodStr = match.Groups["Method"].Value.Trim();
            if (!ExtractSignatures(methodStr, out typeFullName, out methodSignature))
                return false;

            var isILOffset = !string.IsNullOrEmpty(match.Groups["IL"].Value);
            var offsetVarName = (isILOffset) ? "IL" : "NativeOffset";
            var offset = int.Parse(match.Groups[offsetVarName].Value, NumberStyles.HexNumber, CultureInfo.InvariantCulture);

            uint methodIndex = 0xffffff;
            if (!string.IsNullOrEmpty(match.Groups["MethodIndex"].Value))
                methodIndex = uint.Parse(match.Groups["MethodIndex"].Value, CultureInfo.InvariantCulture);

            var mvid = match.Groups["MVID"].Value;
            var aotid = match.Groups["AOTID"].Value;

            stackFrame = new StackFrameData(line, typeFullName, methodSignature, offset, isILOffset, methodIndex, mvid, aotid);

            return true;
        }

        static bool ExtractSignatures(string str, out string typeFullName, out string methodSignature)
        {
            var methodNameEnd = str.IndexOf('(');
            if (methodNameEnd == -1)
            {
                typeFullName = methodSignature = null;
                return false;
            }

            var typeNameEnd = str.LastIndexOf('.', methodNameEnd);
            if (typeNameEnd == -1)
            {
                typeFullName = methodSignature = null;
                return false;
            }

            // Adjustment for Type..ctor ()
            if (typeNameEnd > 0 && str[typeNameEnd - 1] == '.')
            {
                --typeNameEnd;
            }

            typeFullName = str.Substring(0, typeNameEnd);
            // Remove generic parameters
            typeFullName = Regex.Replace(typeFullName, @"\[[^\[\]]*\]$", "");
            typeFullName = Regex.Replace(typeFullName, @"\<[^\[\]]*\>$", "");

            methodSignature = str.Substring(typeNameEnd + 1);

            return true;
        }
    }
}
