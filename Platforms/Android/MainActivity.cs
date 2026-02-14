using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using Android.Webkit;
using AndroidX.Core.Graphics;
using AndroidX.Core.View;

namespace OneManVanFSM
{
    [Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    public class MainActivity : MauiAppCompatActivity
    {
        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            if (Window is { DecorView: { } decorView })
            {
                // Allow app to draw behind system bars so we can control padding ourselves
                WindowCompat.SetDecorFitsSystemWindows(Window, false);

                // Use the WindowInsetsController for system bar colors (non-deprecated approach)
                var controller = WindowCompat.GetInsetsController(Window, decorView);
                if (controller != null)
                {
                    controller.AppearanceLightStatusBars = false;       // light icons on primary header
                    controller.AppearanceLightNavigationBars = true;    // dark icons on white nav bar
                }

#pragma warning disable CA1422 // Platform compatibility — fallback for API < 35
                Window.SetStatusBarColor(Android.Graphics.Color.ParseColor("#0d6efd"));
                Window.SetNavigationBarColor(Android.Graphics.Color.White);
#pragma warning restore CA1422
            }

            // Apply system bar insets as padding on the root content view
            // This physically pushes the BlazorWebView below the status bar and above the nav bar
            var content = FindViewById(Android.Resource.Id.Content);
            if (content != null)
            {
                ViewCompat.SetOnApplyWindowInsetsListener(content, new SystemBarInsetsListener());
            }
        }

#pragma warning disable CA1422 // OnBackPressed is deprecated in API 33+ but still needed for MAUI Blazor back navigation
        public override void OnBackPressed()
        {
            // Find the Android WebView inside the BlazorWebView and navigate back if possible
            var webView = FindWebView(FindViewById(Android.Resource.Id.Content));
            if (webView is not null)
            {
                webView.EvaluateJavascript("window.goBack()", new JavaScriptCallback(navigated =>
                {
                    if (navigated != "true")
                    {
                        MainThread.BeginInvokeOnMainThread(() => base.OnBackPressed());
                    }
                }));
                return;
            }

            base.OnBackPressed();
        }
#pragma warning restore CA1422

        private static Android.Webkit.WebView? FindWebView(Android.Views.View? view)
        {
            if (view is Android.Webkit.WebView wv) return wv;
            if (view is ViewGroup vg)
            {
                for (var i = 0; i < vg.ChildCount; i++)
                {
                    var result = FindWebView(vg.GetChildAt(i));
                    if (result is not null) return result;
                }
            }
            return null;
        }

        private class JavaScriptCallback(Action<string?> callback) : Java.Lang.Object, IValueCallback
        {
            public void OnReceiveValue(Java.Lang.Object? value)
            {
                callback(value?.ToString());
            }
        }

        private class SystemBarInsetsListener : Java.Lang.Object, IOnApplyWindowInsetsListener
        {
            public WindowInsetsCompat OnApplyWindowInsets(Android.Views.View? v, WindowInsetsCompat? insets)
            {
                if (v == null || insets == null)
                    return insets ?? new WindowInsetsCompat.Builder().Build()!;

                var systemBars = insets.GetInsets(WindowInsetsCompat.Type.SystemBars());
                if (systemBars != null)
                {
                    v.SetPadding(systemBars.Left, systemBars.Top, systemBars.Right, systemBars.Bottom);
                }
                return WindowInsetsCompat.Consumed!;
            }
        }
    }
}
