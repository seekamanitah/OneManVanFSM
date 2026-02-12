using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace OneManVanFSM.Web.Services;

public class EmailService(ICompanyProfileService profileSvc) : IEmailService
{
    public async Task<bool> IsConfiguredAsync()
    {
        var profile = await profileSvc.GetProfileAsync();
        return !string.IsNullOrWhiteSpace(profile.SmtpHost)
            && !string.IsNullOrWhiteSpace(profile.Email);
    }

    public async Task SendEmailAsync(EmailMessage message)
    {
        var profile = await profileSvc.GetProfileAsync();

        if (string.IsNullOrWhiteSpace(profile.SmtpHost))
            throw new InvalidOperationException("SMTP is not configured. Go to Settings > Company to set up email.");

        var email = new MimeMessage();
        email.From.Add(new MailboxAddress(profile.Name, profile.Email));
        email.To.Add(MailboxAddress.Parse(message.To));
        email.Subject = message.Subject;

        var builder = new BodyBuilder();
        if (message.IsHtml)
            builder.HtmlBody = message.Body;
        else
            builder.TextBody = message.Body;

        foreach (var att in message.Attachments)
        {
            builder.Attachments.Add(att.FileName, att.Content, ContentType.Parse(att.ContentType));
        }

        email.Body = builder.ToMessageBody();

        using var smtp = new SmtpClient();
        await smtp.ConnectAsync(profile.SmtpHost, profile.SmtpPort,
            profile.SmtpUseSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.Auto);

        if (!string.IsNullOrWhiteSpace(profile.SmtpUsername))
            await smtp.AuthenticateAsync(profile.SmtpUsername, profile.SmtpPassword);

        await smtp.SendAsync(email);
        await smtp.DisconnectAsync(true);
    }
}
