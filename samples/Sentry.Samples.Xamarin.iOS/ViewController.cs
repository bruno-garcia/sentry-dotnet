using Foundation;
using System;
using System.Runtime.CompilerServices;
using UIKit;

namespace Sentry.Samples.Xamarin.iOS
{
    public partial class ViewController : UIViewController
    {
        public ViewController(IntPtr handle) : base(handle)
        {
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();
            // Perform any additional setup after loading the view, typically from a nib.
        }

        public override void DidReceiveMemoryWarning()
        {
            base.DidReceiveMemoryWarning();
            // Release any cached data, images, etc that aren't in use.
        }

        partial void UIButton197_TouchUpInside(UIButton sender)
        {
            try
            {
                PleaseDontThrow();
            }
            catch (Exception ex)
            {
                _ = SentrySdk.CaptureException(ex);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void PleaseDontThrow() => throw new Exception("oops...");
    }
}