using MSMEDigitize.Core.Common;
// EmailService.cs and SmsService.cs — Fixed v6
// These concrete classes implement IEmailService and ISmsService by
// delegating to SendGrid and Twilio respectively.
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MSMEDigitize.Core.Interfaces;
using SendGrid;
using SendGrid.Helpers.Mail;
using Twilio;
using Twilio.Rest.Api.V2010.Account;

namespace MSMEDigitize.Infrastructure.Services;

// ─── Email Service ──────────────────────────────────────────────────────────
public class EmailService : IEmailService
{
    private readonly IConfiguration _config;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration config, ILogger<EmailService> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task SendEmailAsync(string to, string subject, string body, bool isHtml = true)
    {
        try
        {
            var apiKey = _config["SendGrid:ApiKey"];
            var client = new SendGridClient(apiKey);
            var from = new EmailAddress(_config["SendGrid:FromEmail"], _config["SendGrid:FromName"]);
            var toAddress = new EmailAddress(to);
            var msg = MailHelper.CreateSingleEmail(from, toAddress, subject,
                isHtml ? null : body, isHtml ? body : null);
            var response = await client.SendEmailAsync(msg);
            if (!response.IsSuccessStatusCode)
            {
                var err = await response.Body.ReadAsStringAsync();
                _logger.LogWarning("Email send failed to {To}: {Error}", to, err);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Email send error to {To}", to);
        }
    }

    public async Task SendEmailAsync(List<string> to, string subject, string body, bool isHtml = true, List<string>? attachmentPaths = null)
    {
        foreach (var recipient in to)
            await SendEmailAsync(recipient, subject, body, isHtml);
    }

    public async Task SendInvoiceEmailAsync(string to, string customerName, byte[] invoicePdf, string invoiceNumber)
    {
        try
        {
            var apiKey = _config["SendGrid:ApiKey"];
            var client = new SendGridClient(apiKey);
            var from = new EmailAddress(_config["SendGrid:FromEmail"], _config["SendGrid:FromName"]);
            var toAddress = new EmailAddress(to);
            var subject = $"Invoice {invoiceNumber} from {_config["AppName"] ?? "MSMEDigitize"}";
            var body = $"<p>Dear {customerName},</p><p>Please find attached invoice {invoiceNumber}.</p>";
            var msg = MailHelper.CreateSingleEmail(from, toAddress, subject, null, body);
            msg.AddAttachment(new Attachment
            {
                Content = Convert.ToBase64String(invoicePdf),
                Filename = $"Invoice-{invoiceNumber}.pdf",
                Type = "application/pdf",
                Disposition = "attachment"
            });
            await client.SendEmailAsync(msg);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Invoice email error to {To}", to);
        }
    }

    public async Task SendOTPEmailAsync(string to, string otp, string name)
    {
        var body = $"<p>Dear {name},</p><p>Your OTP is: <strong>{otp}</strong>. Valid for 10 minutes.</p>";
        await SendEmailAsync(to, "Your OTP - MSMEDigitize", body);
    }

    public async Task SendWelcomeEmailAsync(string to, string name, string businessName)
    {
        var body = $"<h2>Welcome {name}!</h2><p>Your business <strong>{businessName}</strong> has been registered on MSMEDigitize. Your 14-day free trial has started!</p>";
        await SendEmailAsync(to, "Welcome to MSMEDigitize! 🚀", body);
    }

    public async Task SendPaymentReceiptAsync(string to, string customerName, decimal amount, string receiptNumber)
    {
        var body = $"<p>Dear {customerName},</p><p>Payment of ₹{amount:N2} received. Receipt: {receiptNumber}.</p>";
        await SendEmailAsync(to, $"Payment Receipt {receiptNumber}", body);
    }
}

// ─── SMS Service ────────────────────────────────────────────────────────────
public class SmsService : ISmsService
{
    private readonly IConfiguration _config;
    private readonly ILogger<SmsService> _logger;

    public SmsService(IConfiguration config, ILogger<SmsService> logger)
    {
        _config = config;
        _logger = logger;
    }

    private string FormatIndianNumber(string phone) =>
        phone.StartsWith("+91") ? phone : $"+91{phone.TrimStart('0')}";

    public Task SendSmsAsync(string phone, string message)
    {
        try
        {
            TwilioClient.Init(_config["Twilio:AccountSid"], _config["Twilio:AuthToken"]);
            _ = MessageResource.CreateAsync(
                body: message,
                from: new Twilio.Types.PhoneNumber(_config["Twilio:FromNumber"]),
                to: new Twilio.Types.PhoneNumber(FormatIndianNumber(phone)));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SMS send error to {Phone}", phone);
        }
        return Task.CompletedTask;
    }

    public Task SendOTPAsync(string phone, string otp)
        => SendSmsAsync(phone, $"Your MSMEDigitize OTP is {otp}. Valid for 10 minutes. Do not share.");

    public Task SendPaymentReminderAsync(string phone, string customerName, decimal amount, string invoiceNumber)
        => SendSmsAsync(phone, $"Dear {customerName}, payment of Rs.{amount:N2} is due for invoice {invoiceNumber}. Please pay at the earliest.");

    public Task SendInvoiceLinkAsync(string phone, string customerName, string invoiceLink)
        => SendSmsAsync(phone, $"Dear {customerName}, view/download your invoice: {invoiceLink}");

    public Task SendWhatsAppAsync(string phone, string message, string? templateName = null)
    {
        try
        {
            TwilioClient.Init(_config["Twilio:AccountSid"], _config["Twilio:AuthToken"]);
            var whatsappFrom = $"whatsapp:{_config["Twilio:WhatsAppNumber"] ?? _config["Twilio:FromNumber"]}";
            _ = MessageResource.CreateAsync(
                body: message,
                from: new Twilio.Types.PhoneNumber(whatsappFrom),
                to: new Twilio.Types.PhoneNumber($"whatsapp:{FormatIndianNumber(phone)}"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "WhatsApp send error to {Phone}", phone);
        }
        return Task.CompletedTask;
    }
}

// ─── Null SMS Service (used when Twilio is not configured) ────────────────────
public class NullSmsService : ISmsService
{
    private readonly ILogger<NullSmsService> _logger;
    public NullSmsService(ILogger<NullSmsService> logger) { _logger = logger; }

    public Task SendSmsAsync(string phone, string message)
    { _logger.LogDebug("NullSmsService: SMS to {Phone} suppressed", phone); return Task.CompletedTask; }
    public Task SendOTPAsync(string phone, string otp)
    { _logger.LogDebug("NullSmsService: OTP to {Phone} suppressed", phone); return Task.CompletedTask; }
    public Task SendPaymentReminderAsync(string phone, string customerName, decimal amount, string invoiceNumber)
    { _logger.LogDebug("NullSmsService: Payment reminder to {Phone} suppressed", phone); return Task.CompletedTask; }
    public Task SendInvoiceLinkAsync(string phone, string customerName, string invoiceLink)
    { _logger.LogDebug("NullSmsService: Invoice link to {Phone} suppressed", phone); return Task.CompletedTask; }
    public Task SendWhatsAppAsync(string phone, string message, string? templateName = null)
    { _logger.LogDebug("NullSmsService: WhatsApp to {Phone} suppressed", phone); return Task.CompletedTask; }
}