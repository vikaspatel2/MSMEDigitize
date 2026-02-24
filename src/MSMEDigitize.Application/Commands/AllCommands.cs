using MediatR;
using MSMEDigitize.Application.DTOs;
using MSMEDigitize.Core.Common;
using MSMEDigitize.Core.DTOs;
using MSMEDigitize.Core.Entities.AI;
using MSMEDigitize.Core.Entities.Banking;
using MSMEDigitize.Core.Entities.GST;
using MSMEDigitize.Core.Entities.Inventory;
using MSMEDigitize.Core.Entities.Invoicing;
using MSMEDigitize.Core.Entities.Payroll;
using MSMEDigitize.Core.Entities.Subscriptions;
using MSMEDigitize.Core.Entities.Tenants;
using MSMEDigitize.Core.Enums;
using AIInsightDto = MSMEDigitize.Application.DTOs.AIInsightDto;

namespace MSMEDigitize.Application.Commands.Invoice
{
    // Invoice Commands
    public record CreateInvoiceCommand(Guid TenantId, Guid UserId, CreateInvoiceDto Dto)
        : IRequest<Result<InvoiceDetailDto>>;

    public record UpdateInvoiceCommand(Guid TenantId, Guid InvoiceId, UpdateInvoiceDto Dto)
        : IRequest<Result<InvoiceDetailDto>>;

    public record SendInvoiceCommand(Guid TenantId, Guid InvoiceId, string? CustomMessage)
        : IRequest<Result<bool>>;

    public record RecordPaymentCommand(Guid TenantId, RecordPaymentDto Dto)
        : IRequest<Result<PaymentDto>>;

    public record CancelInvoiceCommand(Guid TenantId, Guid InvoiceId, string Reason)
        : IRequest<Result<bool>>;

    public record GenerateEInvoiceCommand(Guid TenantId, Guid InvoiceId)
        : IRequest<Result<string>>; // Returns IRN

    public record GenerateEWayBillCommand(Guid TenantId, Guid InvoiceId)
        : IRequest<Result<string>>; // Returns EWayBill Number

    public record BulkSendReminderCommand(Guid TenantId, List<Guid> InvoiceIds)
        : IRequest<Result<int>>;
}

namespace MSMEDigitize.Application.Queries.Invoice
{
    public record GetInvoicesQuery(
        Guid TenantId, int Page = 1, int PageSize = 20,
        InvoiceStatus? Status = null, Guid? CustomerId = null,
        DateTime? FromDate = null, DateTime? ToDate = null, string? Search = null)
        : IRequest<Result<PagedResult<InvoiceListDto>>>;

    public record GetInvoiceByIdQuery(Guid TenantId, Guid InvoiceId)
        : IRequest<Result<InvoiceDetailDto>>;

    public record GetInvoicePdfQuery(Guid TenantId, Guid InvoiceId)
        : IRequest<Result<byte[]>>;
}

namespace MSMEDigitize.Application.Commands.Customer
{
    public record CreateCustomerCommand(Guid TenantId, CreateCustomerDto Dto)
        : IRequest<Result<CustomerDto>>;

    public record UpdateCustomerCommand(Guid TenantId, Guid CustomerId, UpdateCustomerDto Dto)
        : IRequest<Result<CustomerDto>>;

    public record DeleteCustomerCommand(Guid TenantId, Guid CustomerId)
        : IRequest<Result<bool>>;
}

namespace MSMEDigitize.Application.Queries.Customer
{
    public record GetCustomersQuery(
        Guid TenantId, int Page = 1, int PageSize = 20, string? Search = null,
        bool? IsActive = null, CustomerType? Type = null)
        : IRequest<Result<PagedResult<CustomerDto>>>;

    public record GetCustomerByIdQuery(Guid TenantId, Guid CustomerId)
        : IRequest<Result<CustomerDto>>;

    public record GetCustomerStatementQuery(Guid TenantId, Guid CustomerId, DateTime From, DateTime To)
        : IRequest<Result<byte[]>>;
}

namespace MSMEDigitize.Application.Commands.Product
{
    public record CreateProductCommand(Guid TenantId, CreateProductDto Dto)
        : IRequest<Result<ProductDto>>;

    public record AdjustStockCommand(Guid TenantId, StockAdjustmentDto Dto)
        : IRequest<Result<bool>>;
}

namespace MSMEDigitize.Application.Queries.Dashboard
{
    public record GetDashboardSummaryQuery(Guid TenantId, string Period = "thisMonth")
        : IRequest<Result<DashboardSummaryDto>>;

    public record GetCashFlowForecastQuery(Guid TenantId, int ForecastDays = 90)
        : IRequest<Result<CashFlowForecastDto>>;
}

namespace MSMEDigitize.Application.Queries.Analytics
{
    public record GetRevenueAnalyticsQuery(Guid TenantId, DateTime From, DateTime To, string GroupBy = "month")
        : IRequest<Result<RevenueAnalyticsDto>>;

    public record GetAIInsightsQuery(Guid TenantId, AIInsightType? Type = null, int Limit = 10)
        : IRequest<Result<IEnumerable<AIInsightDto>>>;
}