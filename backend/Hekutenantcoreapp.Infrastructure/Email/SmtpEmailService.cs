using System.Net;
using System.Net.Mail;
using Hekutenantcoreapp.Domain.Interfaces;
using Microsoft.Extensions.Configuration;

namespace Hekutenantcoreapp.Infrastructure.Email;

public class SmtpEmailService : IEmailService
{
    private readonly IConfiguration _configuration;

    public SmtpEmailService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task SendAsync(string toEmail, string subject, string body)
    {
        var host = _configuration["Email:SmtpHost"];
        var port = int.Parse(_configuration["Email:SmtpPort"] ?? "587");
        var user = _configuration["Email:SmtpUser"];
        var password = _configuration["Email:SmtpPassword"];
        var fromAddress = _configuration["Email:FromAddress"];
        var fromName = _configuration["Email:FromName"];

        using var client = new SmtpClient(host, port)
        {
            Credentials = new NetworkCredential(user, password),
            EnableSsl = true
        };

        var message = new MailMessage
        {
            From = new MailAddress(fromAddress!, fromName),
            Subject = subject,
            Body = body,
            IsBodyHtml = true
        };
        message.To.Add(toEmail);

        await client.SendMailAsync(message);
    }
}