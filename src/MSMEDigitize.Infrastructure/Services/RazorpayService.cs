using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MSMEDigitize.Core.Interfaces;
using Razorpay.Api;
using System.Security.Cryptography;
using System.Text;

namespace MSMEDigitize.Infrastructure.Services;

public class RazorpayPaymentService : IPaymentGatewayService
{
    private readonly string _keyId;
    private readonly string _keySecret;
    private readonly ILogger<RazorpayPaymentService> _logger;

    public RazorpayPaymentService(IConfiguration config, ILogger<RazorpayPaymentService> logger)
    {
        _logger = logger;
        _keyId     = config["Razorpay:KeyId"]     ?? "rzp_test_key";
        _keySecret = config["Razorpay:KeySecret"] ?? "secret";
    }

    private RazorpayClient GetClient() => new RazorpayClient(_keyId, _keySecret);

    public async Task<string> CreateOrderAsync(decimal amount, string currency, string receipt, CancellationToken ct = default)
    {
        await Task.CompletedTask;
        var options = new Dictionary<string, object>
        {
            { "amount",          (int)(amount * 100) },
            { "currency",        currency },
            { "receipt",         receipt },
            { "payment_capture", 1 }
        };
        try
        {
            var order = GetClient().Order.Create(options);
            return order["id"]?.ToString() ?? string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CreateOrderAsync failed");
            throw;
        }
    }

    public async Task<bool> VerifyPaymentAsync(string orderId, string paymentId, string signature, CancellationToken ct = default)
    {
        await Task.CompletedTask;
        try
        {
            var payload = $"{orderId}|{paymentId}";
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_keySecret));
            var hash = BitConverter.ToString(hmac.ComputeHash(Encoding.UTF8.GetBytes(payload))).Replace("-", "").ToLower();
            return hash == signature;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "VerifyPaymentAsync failed");
            return false;
        }
    }

    public async Task<string> CreatePaymentLinkAsync(Guid invoiceId, decimal amount, string description, string customerEmail, CancellationToken ct = default)
    {
        await Task.CompletedTask;
        _logger.LogInformation("CreatePaymentLink for invoice {InvoiceId}, amount {Amount}", invoiceId, amount);
        // Payment link creation - returns a mock URL in demo mode
        return $"https://rzp.io/l/{invoiceId:N}";
    }

    public async Task<string> CreateSubscriptionAsync(Guid tenantId, string planId, CancellationToken ct = default)
    {
        await Task.CompletedTask;
        _logger.LogInformation("CreateSubscription for tenant {TenantId}, plan {PlanId}", tenantId, planId);
        return $"sub_{Guid.NewGuid():N}";
    }

    public async Task<bool> CancelSubscriptionAsync(string subscriptionId, CancellationToken ct = default)
    {
        await Task.CompletedTask;
        _logger.LogInformation("CancelSubscription {SubscriptionId}", subscriptionId);
        return true;
    }
}

public class RazorpayOptions
{
    public string KeyId     { get; set; } = string.Empty;
    public string KeySecret { get; set; } = string.Empty;
    public string WebhookSecret { get; set; } = string.Empty;
}
