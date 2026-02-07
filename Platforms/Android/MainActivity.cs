using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;
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
                controller.AppearanceLightStatusBars = false;       // light icons on primary header
                controller.AppearanceLightNavigationBars = true;    // dark icons on white nav bar

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

        private class SystemBarInsetsListener : Java.Lang.Object, IOnApplyWindowInsetsListener
        {
            public WindowInsetsCompat OnApplyWindowInsets(Android.Views.View? v, WindowInsetsCompat? insets)
            {
                if (v == null || insets == null)
                    return insets ?? new WindowInsetsCompat.Builder().Build();

                var systemBars = insets.GetInsets(WindowInsetsCompat.Type.SystemBars());
                v.SetPadding(systemBars.Left, systemBars.Top, systemBars.Right, systemBars.Bottom);
                return WindowInsetsCompat.Consumed;
            }
        }
    }
}
