using PuppeteerSharp;
using PuppeteerSharp.Media;

namespace OneManVanFSM.Web.Services;

public class PdfService : IPdfService, IAsyncDisposable
{
    private IBrowser? _browser;
    private static readonly SemaphoreSlim _semaphore = new(1, 1);
    private static readonly string _printCssPath = Path.Combine(AppContext.BaseDirectory, "wwwroot", "css", "print.css");

    public async Task EnsureBrowserAsync()
    {
        if (_browser is not null) return;
        await _semaphore.WaitAsync();
        try
        {
            if (_browser is not null) return;

            var browserFetcher = new BrowserFetcher();
            await browserFetcher.DownloadAsync();

            _browser = await Puppeteer.LaunchAsync(new LaunchOptions
            {
                Headless = true,
                Args = ["--no-sandbox", "--disable-setuid-sandbox", "--disable-gpu"],
            });
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<byte[]> GeneratePdfFromHtmlAsync(string htmlContent)
    {
        await EnsureBrowserAsync();

        await using var page = await _browser!.NewPageAsync();
        await page.SetContentAsync(htmlContent, new NavigationOptions
        {
            WaitUntil = [WaitUntilNavigation.Networkidle0],
            Timeout = 30_000,
        });

        var pdfBytes = await page.PdfDataAsync(new PdfOptions
        {
            Format = PaperFormat.Letter,
            PrintBackground = true,
            MarginOptions = new MarginOptions
            {
                Top = "0.3cm",
                Bottom = "0.3cm",
                Left = "1.2cm",
                Right = "1.2cm",
            },
        });

        return pdfBytes;
    }

    /// <summary>
    /// Wraps print-doc HTML content in a full HTML document with the print CSS applied
    /// as a regular (non-media-query) stylesheet so Puppeteer renders it correctly.
    /// </summary>
    public static string WrapInHtmlDocument(string printDocHtml)
    {
        var printCss = File.Exists(_printCssPath)
            ? File.ReadAllText(_printCssPath)
            : "";

        // Strip the @media print { ... } wrapper so styles apply unconditionally
        printCss = StripMediaPrintWrapper(printCss);

        return $$"""
            <!DOCTYPE html>
            <html lang="en">
            <head>
                <meta charset="utf-8" />
                <link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/bootstrap@5.3.3/dist/css/bootstrap.min.css" />
                <link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.3/font/bootstrap-icons.min.css" />
                <style>
                    body { margin: 0; padding: 0; background: #fff; font-family: 'Segoe UI', system-ui, -apple-system, sans-serif; font-size: 9pt; }
                    * { -webkit-print-color-adjust: exact !important; print-color-adjust: exact !important; }
                    {{printCss}}
                </style>
            </head>
            <body>
                {{printDocHtml}}
            </body>
            </html>
            """;
    }

    private static string StripMediaPrintWrapper(string css)
    {
        // Remove the outer @media print { ... } wrapper so styles apply to screen (which is what Puppeteer renders)
        var trimmed = css.Trim();
        if (trimmed.StartsWith("@media print", StringComparison.OrdinalIgnoreCase))
        {
            var braceStart = trimmed.IndexOf('{');
            if (braceStart >= 0)
            {
                // Find matching closing brace
                var depth = 0;
                var lastBrace = -1;
                for (var i = braceStart; i < trimmed.Length; i++)
                {
                    if (trimmed[i] == '{') depth++;
                    else if (trimmed[i] == '}') { depth--; if (depth == 0) { lastBrace = i; break; } }
                }
                if (lastBrace > braceStart)
                    return trimmed[(braceStart + 1)..lastBrace].Trim();
            }
        }
        return trimmed;
    }

    public async ValueTask DisposeAsync()
    {
        if (_browser is not null)
        {
            await _browser.CloseAsync();
            _browser.Dispose();
            _browser = null;
        }
        GC.SuppressFinalize(this);
    }
}
