using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MSMEDigitize.Core.Interfaces;
using System.Net;
using System.Net.Mail;

namespace MSMEDigitize.Infrastructure.Services;

/// <summary>
/// Simple SMTP-based email service with no external package dependencies.
/// Configure via appsettings.json under "Smtp" section.
/// Falls back to log-only mode if SMTP is not configured.
/// </summary>
public class SmtpEmailService : IEmailService
{
    private readonly IConfiguration _config;
    private readonly ILogger<SmtpEmailService> _logger;

    public SmtpEmailService(IConfiguration config, ILogger<SmtpEmailService> logger)
    {
        _config = config;
        _logger = logger;
    }

    private SmtpClient? CreateClient()
    {
        var host = _config["Smtp:Host"];
        if (string.IsNullOrEmpty(host)) return null;

        var client = new SmtpClient(host)
        {
            Port = int.Parse(_config["Smtp:Port"] ?? "587"),
            EnableSsl = bool.Parse(_config["Smtp:EnableSsl"] ?? "true"),
            DeliveryMethod = SmtpDeliveryMethod.Network
        };

        var user = _config["Smtp:Username"];
        var pass = _config["Smtp:Password"];
        if (!string.IsNullOrEmpty(user))
            client.Credentials = new NetworkCredential(user, pass);

        return client;
    }

    public async Task SendEmailAsync(string to, string subject, string body, bool isHtml = true)
    {
        using var client = CreateClient();
        if (client == null) { _logger.LogWarning("SMTP not configured. Email to {To} | Subject: {Subject}", to, subject); return; }
        var from = _config["Smtp:From"] ?? _config["Smtp:Username"] ?? "noreply@msme.local";
        using var msg = new MailMessage(from, to, subject, body) { IsBodyHtml = isHtml };
        await client.SendMailAsync(msg);
        _logger.LogInformation("Email sent to {To} | Subject: {Subject}", to, subject);
    }

    public async Task SendEmailAsync(List<string> to, string subject, string body, bool isHtml = true, List<string>? attachmentPaths = null)
    {
        using var client = CreateClient();
        if (client == null) { _logger.LogWarning("SMTP not configured. Bulk email | Subject: {Subject}", subject); return; }
        var from = _config["Smtp:From"] ?? _config["Smtp:Username"] ?? "noreply@msme.local";
        using var msg = new MailMessage { From = new MailAddress(from), Subject = subject, Body = body, IsBodyHtml = isHtml };
        foreach (var t in to) msg.To.Add(t);
        if (attachmentPaths != null)
            foreach (var path in attachmentPaths.Where(File.Exists))
                msg.Attachments.Add(new Attachment(path));
        await client.SendMailAsync(msg);
    }

    public async Task SendInvoiceEmailAsync(string to, string customerName, byte[] invoicePdf, string invoiceNumber)
    {
        using var client = CreateClient();
        if (client == null) { _logger.LogWarning("SMTP not configured. Invoice email to {To}", to); return; }
        var from = _config["Smtp:From"] ?? "noreply@msme.local";
        using var msg = new MailMessage(from, to, $"Invoice #{invoiceNumber}", $"<p>Dear {customerName},</p><p>Please find your invoice #{invoiceNumber} attached.</p>") { IsBodyHtml = true };
        using var ms = new MemoryStream(invoicePdf);
        msg.Attachments.Add(new Attachment(ms, $"Invoice-{invoiceNumber}.pdf", "application/pdf"));
        await client.SendMailAsync(msg);
    }

    public async Task SendOTPEmailAsync(string to, string otp, string name)
    {
        await SendEmailAsync(to, "Your OTP - MSME Digitize",
            $"<p>Dear {name},</p><p>Your OTP is: <strong>{otp}</strong></p><p>Valid for 10 minutes.</p>");
    }

    public async Task SendWelcomeEmailAsync(string to, string name, string businessName)
    {
        await SendEmailAsync(to, $"Welcome to MSME Digitize, {name}!",
            $"<h2>Welcome {name}!</h2><p>Your business <strong>{businessName}</strong> is now registered. Start your 14-day free trial today.</p>");
    }

    public async Task SendPaymentReceiptAsync(string to, string customerName, decimal amount, string receiptNumber)
    {
        await SendEmailAsync(to, $"Payment Receipt #{receiptNumber}",
            $"<h2>Payment Received</h2><p>Dear {customerName},</p><p>We received your payment of <strong>₹{amount:N0}</strong>. Receipt: {receiptNumber}. Thank you!</p>");
    }
}
