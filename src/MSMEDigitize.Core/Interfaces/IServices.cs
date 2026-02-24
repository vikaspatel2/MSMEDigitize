using MSMEDigitize.Core.DTOs;
using MSMEDigitize.Core.Enums;
using MSMEDigitize.Core.Common;
using MSMEDigitize.Core.Entities.Tenants;
using System.Linq.Expressions;
using MSMEDigitize.Core.Entities.AI;
using MSMEDigitize.Core.Entities.Banking;
using MSMEDigitize.Core.Entities.GST;
using MSMEDigitize.Core.Entities.Inventory;
using MSMEDigitize.Core.Entities.Invoicing;
using MSMEDigitize.Core.Entities.Payroll;
using MSMEDigitize.Core.Entities.Subscriptions;

namespace MSMEDigitize.Core.Interfaces;

public interface IRepository<T> where T : MSMEDigitize.Core.Common.BaseEntity
{
    Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IEnumerable<T>> GetAllAsync(CancellationToken ct = default);
    Task<PagedResult<T>> GetPagedAsync(int pageNumber, int pageSize, Expression<Func<T, bool>>? filter = null, CancellationToken ct = default);
    Task<T> AddAsync(T entity, CancellationToken ct = default);
    Task<T> UpdateAsync(T entity, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
    Task<bool> ExistsAsync(Guid id, CancellationToken ct = default);
    Task<int> CountAsync(Expression<Func<T, bool>>? filter = null, CancellationToken ct = default);
    Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default);
    Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default);
    IQueryable<T> Query();
    Task<IEnumerable<T>> AddRangeAsync(IEnumerable<T> entities, CancellationToken ct = default);
}

public interface ITenantRepository<T> : IRepository<T> where T : MSMEDigitize.Core.Common.TenantEntity
{
    Task<IEnumerable<T>> GetByTenantAsync(Guid tenantId, CancellationToken ct = default);
    Task<PagedResult<T>> GetPagedByTenantAsync(Guid tenantId, int pageNumber, int pageSize, Expression<Func<T, bool>>? filter = null, CancellationToken ct = default);
}

// IUnitOfWork is defined in IUnitOfWork.cs - see that file for the full interface

public interface ITenantContext
{
    Guid TenantId { get; }
    Guid UserId { get; }
    string UserEmail { get; }
    string UserRole { get; }
    bool IsSystemAdmin { get; }
}

public interface ICacheService
{
    Task<T?> GetAsync<T>(string key, CancellationToken ct = default);
    Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken ct = default);
    Task RemoveAsync(string key, CancellationToken ct = default);
    Task RemoveByPatternAsync(string pattern, CancellationToken ct = default);
    Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiry = null, CancellationToken ct = default);
}

public interface IMessageBus
{
    Task PublishAsync<T>(T message, string? topic = null, CancellationToken ct = default) where T : class;
    Task SubscribeAsync<T>(string topic, Func<T, Task> handler, CancellationToken ct = default) where T : class;
}

public interface IFileStorageService
{
    Task<string> UploadAsync(Stream fileStream, string fileName, string contentType, string? folder = null, CancellationToken ct = default);
    Task<Stream> DownloadAsync(string fileKey, CancellationToken ct = default);
    Task DeleteAsync(string fileKey, CancellationToken ct = default);
    string GetPublicUrl(string fileKey);
    Task<string> GetSignedUrlAsync(string fileKey, TimeSpan expiry, CancellationToken ct = default);
}

public interface INotificationService
{
    Task SendEmailAsync(string to, string subject, string body, bool isHtml = true, CancellationToken ct = default);
    Task SendSMSAsync(string mobile, string message, CancellationToken ct = default);
    Task SendWhatsAppAsync(string mobile, string templateName, Dictionary<string, string> parameters, CancellationToken ct = default);
    Task SendPushNotificationAsync(Guid userId, string title, string body, Dictionary<string, string>? data = null, CancellationToken ct = default);
    Task SendInAppNotificationAsync(Guid tenantId, Guid? userId, string title, string body, string? actionUrl = null, CancellationToken ct = default);
    //Task SendPaymentReceiptAsync(string to, string customerName, decimal amount, string receiptNumber, CancellationToken ct = default);
}

public interface IGSTService
{
    Task<bool> ValidateGSTINAsync(string gstin, CancellationToken ct = default);
    Task<GSTINDetails?> GetGSTINDetailsAsync(string gstin, CancellationToken ct = default);
    Task<bool> GenerateEInvoiceAsync(Guid invoiceId, CancellationToken ct = default);
    Task<bool> CancelEInvoiceAsync(string irn, string reason, CancellationToken ct = default);
    Task<bool> GenerateEWayBillAsync(Guid invoiceId, CancellationToken ct = default);
    Task<string> PrepareGSTR1JsonAsync(Guid tenantId, int year, int month, CancellationToken ct = default);
    Task<bool> FileGSTReturnAsync(Guid returnId, CancellationToken ct = default);
    Task<Result<GSTSummaryDto>> GetSummaryAsync(Guid tenantId, int month, int year, CancellationToken ct = default);
    Task<Result<GSTR1SummaryDto>> GetGSTR1Async(Guid tenantId, int month, int year, CancellationToken ct = default);
    Task<Result<GSTR3BSummaryDto>> GetGSTR3BAsync(Guid tenantId, int month, int year, CancellationToken ct = default);
    Task<Result<IEnumerable<HSNSummaryDto>>> SearchHSNAsync(string query, CancellationToken ct = default);
    Task<Result<ITCReconciliationDto>> GetITCReconciliationAsync(Guid tenantId, int month, int year, CancellationToken ct = default);
    Task<decimal> CalculateCessAsync(string hsnCode, decimal value, CancellationToken ct = default);
}

public record GSTINDetails(string GSTIN, string LegalName, string TradeName, string Status, string StateCode, string RegistrationDate, bool IsActive);

public interface IAIService
{
    Task<decimal> PredictCashFlowAsync(Guid tenantId, int daysAhead, CancellationToken ct = default);
    Task<IEnumerable<Core.Entities.AI.AIInsight>> GenerateInsightsAsync(Guid tenantId, CancellationToken ct = default);
    Task<IEnumerable<AIInsightDto>> GetInsightsAsync(Guid tenantId, AIInsightType? type = null, int limit = 10, CancellationToken ct = default);
    Task<decimal> PredictCustomerChurnAsync(Guid customerId, CancellationToken ct = default);
    Task<decimal> GetOptimalPriceAsync(Guid productId, CancellationToken ct = default);
    Task<decimal> ForecastInventoryDemandAsync(Guid productId, int days, CancellationToken ct = default);
    Task<bool> DetectFraudAsync(Guid transactionId, string transactionType, CancellationToken ct = default);
    Task<decimal> CalculateLoanEligibilityAsync(Guid tenantId, CancellationToken ct = default);
}

public interface IPaymentGatewayService
{
    Task<string> CreateOrderAsync(decimal amount, string currency, string receipt, CancellationToken ct = default);
    Task<bool> VerifyPaymentAsync(string orderId, string paymentId, string signature, CancellationToken ct = default);
    Task<string> CreatePaymentLinkAsync(Guid invoiceId, decimal amount, string description, string customerEmail, CancellationToken ct = default);
    Task<string> CreateSubscriptionAsync(Guid tenantId, string planId, CancellationToken ct = default);
    Task<bool> CancelSubscriptionAsync(string subscriptionId, CancellationToken ct = default);
}

public interface IBankingService
{
    Task<IEnumerable<Entities.Banking.BankTransaction>> FetchStatementAsync(Guid bankAccountId, DateTime from, DateTime to, CancellationToken ct = default);
    Task<bool> AutoReconcileAsync(Guid bankAccountId, CancellationToken ct = default);
    Task<decimal> GetBalanceAsync(Guid bankAccountId, CancellationToken ct = default);
   
}

public interface IPDFService
{
    Task<byte[]> GenerateInvoicePDFAsync(Guid invoiceId, CancellationToken ct = default);
    Task<byte[]> GeneratePayslipPDFAsync(Guid payslipId, CancellationToken ct = default);
    Task<byte[]> GenerateGSTReturnPDFAsync(Guid returnId, CancellationToken ct = default);
    Task<byte[]> GenerateFinancialReportPDFAsync(Guid tenantId, DateTime from, DateTime to, CancellationToken ct = default);
    Task<byte[]> GeneratePurchaseOrderPDFAsync(Guid poId, CancellationToken ct = default);
}

public interface ITallyIntegrationService
{
    Task<bool> ExportToTallyAsync(Guid tenantId, DateTime from, DateTime to, CancellationToken ct = default);
    Task<bool> SyncFromTallyAsync(Guid tenantId, CancellationToken ct = default);
}


// Alias to support both naming conventions
public interface IPdfService : IPDFService { }