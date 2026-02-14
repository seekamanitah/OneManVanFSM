namespace OneManVanFSM.Web.Services;

/// <summary>
/// Generates QR code images for assets.
/// </summary>
public interface IQrCodeService
{
    /// <summary>
    /// Generates a QR code PNG image for a single asset.
    /// Returns the raw PNG bytes.
    /// </summary>
    byte[] GenerateAssetQrPng(int assetId, string assetName, string? serialNumber, int pixelsPerModule = 10);

    /// <summary>
    /// Generates a printable label sheet (single-page HTML) containing QR codes
    /// for multiple assets. Returns UTF-8 HTML string ready for print.
    /// </summary>
    string GenerateQrLabelSheetHtml(List<QrLabelInfo> assets, int pixelsPerModule = 6);
}

public class QrLabelInfo
{
    public int AssetId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? SerialNumber { get; set; }
    public string? AssetType { get; set; }
    public string? SiteName { get; set; }
    public string? CustomerName { get; set; }
}
