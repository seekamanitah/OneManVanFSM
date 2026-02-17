namespace OneManVanFSM
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();
        }

        protected override bool OnBackButtonPressed()
        {
#if ANDROID
            // Use the BlazorWebView's underlying platform WebView to navigate back
            if (blazorWebView?.Handler?.PlatformView is Android.Webkit.WebView webView && webView.CanGoBack())
            {
                webView.GoBack();
                return true;
            }
#endif
            return base.OnBackButtonPressed();
        }
    }
}
