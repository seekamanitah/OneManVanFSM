namespace OneManVanFSM.Services;

public interface IMobilePhotoService
{
    /// <summary>
    /// Captures a photo using the device camera and saves it to app storage.
    /// Returns the local file path or null if cancelled.
    /// </summary>
    Task<PhotoResult?> CapturePhotoAsync();

    /// <summary>
    /// Picks a photo from the device gallery and saves it to app storage.
    /// Returns the local file path or null if cancelled.
    /// </summary>
    Task<PhotoResult?> PickPhotoAsync();
}

public class PhotoResult
{
    public string FilePath { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public long FileSize { get; set; }
}
