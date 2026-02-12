namespace OneManVanFSM.Web.Services;

public interface IPdfService
{
    Task<byte[]> GeneratePdfFromHtmlAsync(string htmlContent);
    Task EnsureBrowserAsync();
}
