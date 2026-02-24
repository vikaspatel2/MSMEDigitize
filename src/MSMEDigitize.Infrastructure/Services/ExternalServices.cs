using System.Net.Http.Json;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MSMEDigitize.Core.Interfaces;

namespace MSMEDigitize.Infrastructure.ExternalServices;

public class EmailService : IEmailService
{
    private readonly IConfiguration _config;
    private readonly ILogger<EmailService> _logger;
    private readonly HttpClient _http;

    public EmailService(
        IConfiguration config,
        ILogger<EmailService> logger,
        IHttpClientFactory factory)
    {
        _config = config;
        _logger = logger;

        _http = factory.CreateClient("SendGrid");
        _http.BaseAddress = new Uri("https://api.sendgrid.com/v3/");
        _http.DefaultRequestHeaders.Add("Authorization",
            $"Bearer {config["SendGrid:ApiKey"]}");
    }

    // ───────────────── EMAIL METHODS ─────────────────

    public async Task SendEmailAsync(string to, string subject, string body, bool isHtml = true)
    {
        await SendEmailAsync(new List<string> { to }, subject, body, isHtml, null);
    }

    public async Task SendEmailAsync(
        List<string> to,
        string subject,
        string body,
        bool isHtml = true,
        List<string>? attachmentPaths = null)
    {
        try
        {
            var payload = new
            {
                personalizations = new[]
                {
                    new
                    {
                        to = to.Select(x => new { email = x }).ToArray()
                    }
                },
                from = new
                {
                    email = _config["SendGrid:From"] ?? "noreply@msme.app"
                },
                subject,
                content = new[]
                {
                    new
                    {
                        type = isHtml ? "text/html" : "text/plain",
                        value = body
                    }
                }
            };

            var resp = await _http.PostAsJsonAsync("mail/send", payload);

            if (!resp.IsSuccessStatusCode)
                _logger.LogWarning("SendGrid failed: {Status}", resp.StatusCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Email sending failed");
        }
    }

    public async Task SendInvoiceEmailAsync(
        string to,
        string customerName,
        byte[] invoicePdf,
        string invoiceNumber)
    {
        var body = $"Hi {customerName},<br/>Please find your invoice #{invoiceNumber}.";
        await SendEmailAsync(to, $"Invoice #{invoiceNumber}", body, true);
        // Attachments can be added later via SendGrid attachment payload
    }

    public async Task SendOTPEmailAsync(string to, string otp, string name)
    {
        var body = $"Hi {name},<br/>Your OTP is <b>{otp}</b>. Valid for 10 minutes.";
        await SendEmailAsync(to, "Your OTP Code", body, true);
    }

    public async Task SendWelcomeEmailAsync(string to, string name, string businessName)
    {
        var body = $"Welcome {name}!<br/>Your business <b>{businessName}</b> is successfully registered.";
        await SendEmailAsync(to, "Welcome to MSMEDigitize", body, true);
    }

    public async Task SendPaymentReceiptAsync(
        string to,
        string customerName,
        decimal amount,
        string receiptNumber)
    {
        var body = $"Hi {customerName},<br/>We received ₹{amount}. Receipt No: {receiptNumber}.";
        await SendEmailAsync(to, "Payment Receipt", body, true);
    }

    // ───────────────── SMS METHODS (Stubbed) ─────────────────
    // These should ideally be in SmsService, not here.

    public Task SendSmsAsync(string phone, string message)
        => Task.CompletedTask;

    public Task SendOTPAsync(string phone, string otp)
        => Task.CompletedTask;

    public Task SendPaymentReminderAsync(string phone, string customerName, decimal amount, string invoiceNumber)
        => Task.CompletedTask;

    public Task SendInvoiceLinkAsync(string phone, string customerName, string invoiceLink)
        => Task.CompletedTask;

    public Task SendWhatsAppAsync(string phone, string message, string? templateName = null)
        => Task.CompletedTask;
}

// ─── SMS Service (Twilio) ─────────────────────────────────────────────────────

public class SmsService : ISmsService
{
    private readonly IConfiguration _config;
    private readonly ILogger<SmsService> _logger;
    private readonly HttpClient _http;

    public SmsService(IConfiguration config, ILogger<SmsService> logger, IHttpClientFactory factory)
    {
        _config = config;
        _logger = logger;

        _http = factory.CreateClient("Twilio");

        var sid = config["Twilio:AccountSid"] ?? "";
        var token = config["Twilio:AuthToken"] ?? "";
        var creds = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{sid}:{token}"));

        _http.BaseAddress = new Uri($"https://api.twilio.com/2010-04-01/Accounts/{sid}/");
        _http.DefaultRequestHeaders.Add("Authorization", $"Basic {creds}");
    }

    public async Task SendSmsAsync(string phone, string message)
    {
        try
        {
            var form = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["To"] = phone,
                ["From"] = _config["Twilio:From"] ?? "",
                ["Body"] = message
            });

            await _http.PostAsync("Messages.json", form);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SMS failed");
        }
    }

    public async Task SendOTPAsync(string phone, string otp)
    {
        await SendSmsAsync(phone, $"Your OTP is {otp}. Valid for 10 minutes.");
    }

    public async Task SendPaymentReminderAsync(string phone, string customerName, decimal amount, string invoiceNumber)
    {
        await SendSmsAsync(phone,
            $"Hi {customerName}, reminder: ₹{amount} pending for Invoice #{invoiceNumber}.");
    }

    public async Task SendInvoiceLinkAsync(string phone, string customerName, string invoiceLink)
    {
        await SendSmsAsync(phone,
            $"Hi {customerName}, view your invoice here: {invoiceLink}");
    }

    public async Task SendWhatsAppAsync(string phone, string message, string? templateName = null)
    {
        // For Twilio WhatsApp you must prefix number with whatsapp:
        await SendSmsAsync($"whatsapp:{phone}", message);
    }
}