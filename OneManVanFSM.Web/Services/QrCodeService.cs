using QRCoder;

namespace OneManVanFSM.Web.Services;

/// <summary>
/// QR code generation service using the QRCoder library.
/// Encodes asset URLs as /assets/{id} so the QR code works from any base URL.
/// </summary>
public class QrCodeService : IQrCodeService
{
    public byte[] GenerateAssetQrPng(int assetId, string assetName, string? serialNumber, int pixelsPerModule = 10)
    {
        var payload = $"/assets/{assetId}";

        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(payload, QRCodeGenerator.ECCLevel.M);
        using var code = new PngByteQRCode(data);
        return code.GetGraphic(pixelsPerModule);
    }

    public string GenerateQrLabelSheetHtml(List<QrLabelInfo> assets, int pixelsPerModule = 6)
    {
        var labels = new System.Text.StringBuilder();

        foreach (var asset in assets)
        {
            var pngBytes = GenerateAssetQrPng(asset.AssetId, asset.Name, asset.SerialNumber, pixelsPerModule);
            var base64 = Convert.ToBase64String(pngBytes);

            labels.AppendLine("<div class=\"qr-label\">");
            labels.AppendLine($"  <img src=\"data:image/png;base64,{base64}\" alt=\"QR {asset.AssetId}\" />");
            labels.AppendLine("  <div class=\"qr-label-text\">");
            labels.AppendLine($"    <strong>{System.Net.WebUtility.HtmlEncode(asset.Name)}</strong>");
            if (!string.IsNullOrEmpty(asset.SerialNumber))
                labels.AppendLine($"    <span>SN: {System.Net.WebUtility.HtmlEncode(asset.SerialNumber)}</span>");
            if (!string.IsNullOrEmpty(asset.AssetType))
                labels.AppendLine($"    <span>{System.Net.WebUtility.HtmlEncode(asset.AssetType)}</span>");
            if (!string.IsNullOrEmpty(asset.SiteName))
                labels.AppendLine($"    <span>{System.Net.WebUtility.HtmlEncode(asset.SiteName)}</span>");
            labels.AppendLine("  </div>");
            labels.AppendLine("</div>");
        }

        return $$"""
            <!DOCTYPE html>
            <html>
            <head>
            <meta charset="utf-8" />
            <title>Asset QR Labels</title>
            <style>
                @media print {
                    body { margin: 0; }
                    .qr-label { break-inside: avoid; }
                }
                body {
                    font-family: 'Segoe UI', Arial, sans-serif;
                    margin: 0.5in;
                    color: #222;
                }
                h1 {
                    font-size: 14pt;
                    margin-bottom: 12pt;
                    border-bottom: 1px solid #ccc;
                    padding-bottom: 4pt;
                }
                .qr-grid {
                    display: grid;
                    grid-template-columns: repeat(3, 1fr);
                    gap: 16px;
                }
                .qr-label {
                    border: 1px solid #ddd;
                    border-radius: 6px;
                    padding: 10px;
                    display: flex;
                    align-items: center;
                    gap: 10px;
                }
                .qr-label img {
                    width: 90px;
                    height: 90px;
                    flex-shrink: 0;
                }
                .qr-label-text {
                    display: flex;
                    flex-direction: column;
                    font-size: 9pt;
                    line-height: 1.3;
                    overflow: hidden;
                }
                .qr-label-text strong {
                    font-size: 10pt;
                    margin-bottom: 2px;
                }
            </style>
            </head>
            <body>
            <h1>Asset QR Code Labels</h1>
            <div class="qr-grid">
            {{labels}}
            </div>
            <script>window.onload = function() { window.print(); };</script>
            </body>
            </html>
            """;
    }
}
