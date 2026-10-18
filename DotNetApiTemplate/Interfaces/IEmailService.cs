namespace DotNetApiTemplate.Interfaces
{
    public interface IEmailService
    {
        Task SendEmailAsync(
        string recipientEmail,
        string subject,
        string htmlBody);
    }
}
