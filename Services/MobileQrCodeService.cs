using QRCoder;

namespace OneManVanFSM.Services;

public class MobileQrCodeService : IMobileQrCodeService
{
    public async Task<string> GenerateAssetQrAsync(int assetId, string assetName, string? serialNumber)
    {
        var payload = $"asset:{assetId}|{assetName}";
        if (!string.IsNullOrWhiteSpace(serialNumber))
            payload += $"|SN:{serialNumber}";

        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(payload, QRCodeGenerator.ECCLevel.M);
        using var code = new PngByteQRCode(data);
        var pngBytes = code.GetGraphic(10);

        var fileName = $"QR_Asset_{assetId}_{DateTime.Now:yyyyMMdd_HHmmss}.png";
        var filePath = Path.Combine(FileSystem.CacheDirectory, fileName);
        await File.WriteAllBytesAsync(filePath, pngBytes);

        return filePath;
    }

    public async Task ShareAssetQrAsync(int assetId, string assetName, string? serialNumber)
    {
        var filePath = await GenerateAssetQrAsync(assetId, assetName, serialNumber);

        await Share.Default.RequestAsync(new ShareFileRequest
        {
            Title = $"QR Code — {assetName}",
            File = new ShareFile(filePath)
        });
    }
}
