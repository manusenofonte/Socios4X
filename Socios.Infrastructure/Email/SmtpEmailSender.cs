using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using Socios.Application.Interfaces;

namespace Socios.Infrastructure.Email;

// Envía correos por SMTP (por ejemplo Gmail con una "contraseña de aplicación").
// Se configura con Email:Smtp:Host, Port, User, Password, From y FromName.
public class SmtpEmailSender : IEmailSender
{
    private readonly string? _host;
    private readonly int _port;
    private readonly string? _user;
    private readonly string? _password;
    private readonly string? _from;
    private readonly string _fromName;

    public SmtpEmailSender(IConfiguration configuration)
    {
        var section = configuration.GetSection("Email:Smtp");
        _host = section["Host"];
        _port = int.TryParse(section["Port"], out var port) ? port : 587;
        _user = section["User"];
        _password = section["Password"];
        _from = string.IsNullOrWhiteSpace(section["From"]) ? _user : section["From"];
        _fromName = string.IsNullOrWhiteSpace(section["FromName"]) ? "Asistente Virtual 4x" : section["FromName"]!;
    }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_host) && !string.IsNullOrWhiteSpace(_from);

    public async Task SendAsync(string to, string subject, string htmlBody, CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
            throw new InvalidOperationException("No hay una cuenta de correo configurada (Email:Smtp).");

        using var message = new MailMessage
        {
            From = new MailAddress(_from!, _fromName),
            Subject = subject,
            Body = htmlBody,
            IsBodyHtml = true
        };
        message.To.Add(to);

        using var client = new SmtpClient(_host, _port)
        {
            EnableSsl = true,
            DeliveryMethod = SmtpDeliveryMethod.Network
        };

        if (!string.IsNullOrWhiteSpace(_user))
            client.Credentials = new NetworkCredential(_user, _password);

        await client.SendMailAsync(message, cancellationToken);
    }
}
