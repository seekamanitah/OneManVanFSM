namespace OneManVanFSM.Services;

public interface IMobileQrCodeService
{
    /// <summary>
    /// Generates a QR code PNG for an asset and returns the file path.
    /// </summary>
    Task<string> GenerateAssetQrAsync(int assetId, string assetName, string? serialNumber);

    /// <summary>
    /// Generates a QR code PNG and shares it via the native share sheet.
    /// </summary>
    Task ShareAssetQrAsync(int assetId, string assetName, string? serialNumber);
}
