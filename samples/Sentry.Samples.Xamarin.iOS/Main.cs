using Sentry.Xamarin;
using UIKit;

namespace Sentry.Samples.Xamarin.iOS
{
    public class Application
    {
        // This is the main entry point of the application.
        static void Main(string[] args)
        {
            SentryXamarin.Init(o =>
            {
                o.Dsn = new Dsn("https://5fd7a6cda8444965bade9ccfd3df9882@sentry.io/1188141");
            });

            // if you want to use a different Application Delegate class from "AppDelegate"
            // you can specify it here.
            UIApplication.Main(args, null, "AppDelegate");
        }
    }
}