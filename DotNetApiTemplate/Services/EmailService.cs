using System.Net;
using DotNetApiTemplate.Interfaces;
using DotNetApiTemplate.Models.Settings;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace DotNetApiTemplate.Services;

public class EmailService : IEmailService
{
    private readonly EmailSettings _settings;

    public EmailService(IOptions<EmailSettings> settings)
    {
        _settings = settings.Value;
    }

    public async Task SendEmailAsync(
        string recipientEmail,
        string subject,
        string htmlBody)
    {
        var email = new MimeMessage();

        email.From.Add(
            new MailboxAddress(
                _settings.SenderName,
                _settings.FromEmail));

        email.To.Add(
            new MailboxAddress(
                recipientEmail,
                recipientEmail));

        email.Subject = subject;

        email.Body = new BodyBuilder
        {
            HtmlBody = htmlBody
        }.ToMessageBody();

        using var smtp = new SmtpClient();

        await smtp.ConnectAsync(
            _settings.SmtpServer,
            _settings.Port,
            SecureSocketOptions.StartTls);

        await smtp.AuthenticateAsync(
            _settings.Username,
            _settings.Password);

        await smtp.SendAsync(email);

        await smtp.DisconnectAsync(true);
    }
}