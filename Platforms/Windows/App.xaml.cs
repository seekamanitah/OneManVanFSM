using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Windows.Graphics;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace OneManVanFSM.WinUI
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : MauiWinUIApplication
    {
        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            this.InitializeComponent();
        }

        protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();

        protected override void OnLaunched(LaunchActivatedEventArgs args)
        {
            base.OnLaunched(args);

            // Configure window size and title
            var window = Application.Windows[0].Handler?.PlatformView as Microsoft.UI.Xaml.Window;
            if (window is not null)
            {
                window.Title = "OneManVanFSM — Field Service Management";

                var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
                var windowId = Win32Interop.GetWindowIdFromWindow(hwnd);
                var appWindow = AppWindow.GetFromWindowId(windowId);

                // Set default size: 1400 x 900
                appWindow.Resize(new SizeInt32(1400, 900));

                // Center on screen
                var displayArea = DisplayArea.GetFromWindowId(windowId, DisplayAreaFallback.Primary);
                if (displayArea is not null)
                {
                    var centerX = (displayArea.WorkArea.Width - 1400) / 2;
                    var centerY = (displayArea.WorkArea.Height - 900) / 2;
                    appWindow.Move(new PointInt32(centerX, centerY));
                }
            }
        }
    }

}
