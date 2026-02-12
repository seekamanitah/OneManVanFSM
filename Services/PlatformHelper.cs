namespace OneManVanFSM.Services;

/// <summary>
/// Detects the current platform so the app can switch between
/// DesktopLayout (Windows) and MobileLayout (Android).
/// </summary>
public interface IPlatformHelper
{
    bool IsDesktop { get; }
    bool IsMobile { get; }
    string PlatformName { get; }
}

public class PlatformHelper : IPlatformHelper
{
    public bool IsDesktop =>
        DeviceInfo.Current.Platform == DevicePlatform.WinUI
        || DeviceInfo.Current.Platform == DevicePlatform.macOS
        || DeviceInfo.Current.Idiom == DeviceIdiom.Desktop;

    public bool IsMobile =>
        DeviceInfo.Current.Platform == DevicePlatform.Android
        || DeviceInfo.Current.Platform == DevicePlatform.iOS
        || DeviceInfo.Current.Idiom == DeviceIdiom.Phone;

    public string PlatformName => DeviceInfo.Current.Platform.ToString();
}
