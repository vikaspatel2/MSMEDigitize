namespace MSMEDigitize.Core.Interfaces;

public interface ISmsService
{
    Task SendSmsAsync(string phone, string message);
    Task SendOTPAsync(string phone, string otp);
    Task SendPaymentReminderAsync(string phone, string customerName, decimal amount, string invoiceNumber);
    Task SendInvoiceLinkAsync(string phone, string customerName, string invoiceLink);
    Task SendWhatsAppAsync(string phone, string message, string? templateName = null);
}
