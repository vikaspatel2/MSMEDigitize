namespace MSMEDigitize.Core.DTOs;

public class PagedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber < TotalPages;
}

public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public T? Data { get; set; }
    public List<string> Errors { get; set; } = new();

    public static ApiResponse<T> Ok(T data, string? message = null) =>
        new() { Success = true, Data = data, Message = message };

    public static ApiResponse<T> Fail(string error) =>
        new() { Success = false, Errors = new List<string> { error } };

    public static ApiResponse<T> Fail(List<string> errors) =>
        new() { Success = false, Errors = errors };
}

public class CreateOrderResponse { public string OrderId { get; set; } = string.Empty; public string Currency { get; set; } = "INR"; public decimal Amount { get; set; } public string? KeyId { get; set; } }
public class PaymentDetailsResponse { public string PaymentId { get; set; } = string.Empty; public string? OrderId { get; set; } public decimal Amount { get; set; } public string Status { get; set; } = string.Empty; public string Method { get; set; } = string.Empty; }
public class RefundResponse { public string RefundId { get; set; } = string.Empty; public decimal Amount { get; set; } public string Status { get; set; } = string.Empty; }

public class DashboardMetricsDto
{
    public decimal TotalRevenue { get; set; }
    public decimal MonthlyRevenue { get; set; }
    public decimal OutstandingAmount { get; set; }
    public decimal OverdueAmount { get; set; }
    public int TotalInvoices { get; set; }
    public int PendingInvoices { get; set; }
    public int TotalCustomers { get; set; }
    public int NewCustomersThisMonth { get; set; }
    public int TotalProducts { get; set; }
    public int LowStockProducts { get; set; }
    public int TotalEmployees { get; set; }
    public List<MonthlyRevenueDto> MonthlyRevenueChart { get; set; } = new();
    public List<TopCustomerDto> TopCustomers { get; set; } = new();
    public List<RecentInvoiceDto> RecentInvoices { get; set; } = new();
    public List<StockAlertDto> StockAlerts { get; set; } = new();
    public decimal GSTPayable { get; set; }
    public decimal CashInHand { get; set; }
    public decimal BankBalance { get; set; }
    public int PendingLeaveRequests { get; set; }
}

public class MonthlyRevenueDto { public string Month { get; set; } = string.Empty; public decimal Revenue { get; set; } public decimal Expenses { get; set; } public decimal Profit { get; set; } }
// TopCustomerDto is defined in ExtendedDTOs.cs (with 4-arg constructor)
public class RecentInvoiceDto { public string InvoiceNumber { get; set; } = string.Empty; public string CustomerName { get; set; } = string.Empty; public decimal Amount { get; set; } public string Status { get; set; } = string.Empty; public DateTime Date { get; set; } }
public class StockAlertDto { public string ProductName { get; set; } = string.Empty; public decimal CurrentStock { get; set; } public decimal MinStock { get; set; } public string Unit { get; set; } = string.Empty; }

// Dashboard view models
public class DashboardSummary
{
    public decimal TotalRevenue { get; set; }
    public decimal TotalExpenses { get; set; }
    public decimal NetProfit { get; set; }
    public decimal OutstandingReceivables { get; set; }
    public int TotalInvoices { get; set; }
    public int OverdueInvoices { get; set; }
    public int LowStockItems { get; set; }
    public decimal GSTPayable { get; set; }
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
}

public class SalesDataPoint
{
    public DateTime Date { get; set; }
    public decimal Amount { get; set; }
    public string Label { get; set; } = string.Empty;
}