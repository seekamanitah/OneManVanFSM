namespace OneManVanFSM.Web.Services;

public interface IEmailService
{
    Task SendEmailAsync(EmailMessage message);
    Task<bool> IsConfiguredAsync();
}

public class EmailMessage
{
    public string To { get; set; } = "";
    public string Subject { get; set; } = "";
    public string Body { get; set; } = "";
    public bool IsHtml { get; set; }
    public List<EmailAttachment> Attachments { get; set; } = [];
}

public class EmailAttachment
{
    public string FileName { get; set; } = "";
    public byte[] Content { get; set; } = [];
    public string ContentType { get; set; } = "application/pdf";
}
