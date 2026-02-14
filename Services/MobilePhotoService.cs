namespace OneManVanFSM.Services;

public class MobilePhotoService : IMobilePhotoService
{
    private readonly string _photoDir;

    public MobilePhotoService()
    {
        _photoDir = Path.Combine(FileSystem.AppDataDirectory, "photos");
        Directory.CreateDirectory(_photoDir);
    }

    public async Task<PhotoResult?> CapturePhotoAsync()
    {
        if (!MediaPicker.Default.IsCaptureSupported)
            return null;

        var photo = await MediaPicker.Default.CapturePhotoAsync();
        return await SavePhotoAsync(photo);
    }

    public async Task<PhotoResult?> PickPhotoAsync()
    {
        var photo = await MediaPicker.Default.PickPhotoAsync(new MediaPickerOptions
        {
            Title = "Select a photo"
        });
        return await SavePhotoAsync(photo);
    }

    private async Task<PhotoResult?> SavePhotoAsync(FileResult? photo)
    {
        if (photo is null)
            return null;

        var fileName = $"photo_{DateTime.Now:yyyyMMdd_HHmmss}_{Path.GetFileName(photo.FileName)}";
        var destPath = Path.Combine(_photoDir, fileName);

        await using var sourceStream = await photo.OpenReadAsync();
        await using var destStream = File.Create(destPath);
        await sourceStream.CopyToAsync(destStream);

        var fileInfo = new FileInfo(destPath);

        return new PhotoResult
        {
            FilePath = destPath,
            FileName = fileName,
            FileSize = fileInfo.Length
        };
    }
}
