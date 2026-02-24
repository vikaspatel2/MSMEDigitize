namespace MSMEDigitize.Core.Interfaces;

public interface IEmailService
{
    Task SendEmailAsync(string to, string subject, string body, bool isHtml = true);
    Task SendEmailAsync(List<string> to, string subject, string body, bool isHtml = true, List<string>? attachmentPaths = null);

    Task SendInvoiceEmailAsync(string to, string customerName, byte[] invoicePdf, string invoiceNumber);
    Task SendOTPEmailAsync(string to, string otp, string name);
    Task SendWelcomeEmailAsync(string to, string name, string businessName);
    Task SendPaymentReceiptAsync(string to, string customerName, decimal amount, string receiptNumber);
}

//namespace MSMEDigitize.Core.Interfaces;

//public interface IEmailService
//{
//    Task SendEmailAsync(string to, string subject, string body, bool isHtml = true);
//    Task SendEmailAsync(List<string> to, string subject, string body, bool isHtml = true, List<string>? attachmentPaths = null);
//    Task SendInvoiceEmailAsync(string to, string customerName, byte[] invoicePdf, string invoiceNumber);
//    Task SendOTPEmailAsync(string to, string otp, string name);
//    Task SendWelcomeEmailAsync(string to, string name, string businessName);
//    Task SendPaymentReceiptAsync(string to, string customerName, decimal amount, string receiptNumber);

//    Task SendSmsAsync(string phone, string message);
//    Task SendOTPAsync(string phone, string otp);
//    Task SendPaymentReminderAsync(string phone, string customerName, decimal amount, string invoiceNumber);
//    Task SendInvoiceLinkAsync(string phone, string customerName, string invoiceLink);
//    Task SendWhatsAppAsync(string phone, string message, string? templateName = null);
//}
