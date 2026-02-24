using MSMEDigitize.Core.Common;
using MSMEDigitize.Core.Enums;
using MSMEDigitize.Core.Entities.AI;
using MSMEDigitize.Core.Entities.GST;
using MSMEDigitize.Core.Entities.Inventory;
using MSMEDigitize.Core.Entities.Invoicing;
using MSMEDigitize.Core.Entities.Payroll;
using MSMEDigitize.Core.Entities.Subscriptions;
using MSMEDigitize.Core.Entities.Tenants;

namespace MSMEDigitize.Core.Entities.Banking;

public class BankAccount : TenantEntity
{
    public string AccountName { get; set; } = string.Empty;
    public string AccountNumber { get; set; } = string.Empty; // encrypted
    public string IFSC { get; set; } = string.Empty;
    public string BankName { get; set; } = string.Empty;
    public string BranchName { get; set; } = string.Empty;
    public string AccountType { get; set; } = "Current"; // Current, Savings, OD, CC
    public decimal OpeningBalance { get; set; }
    public decimal CurrentBalance { get; set; }
    public decimal Balance => CurrentBalance;
    public decimal? OverdraftLimit { get; set; }
    public bool IsPrimary { get; set; }
    public bool IsActive { get; set; } = true;
    public string? EncryptedCredentials { get; set; } // for bank statement auto-fetch
    public DateTime? LastSyncAt { get; set; }
    public string? BankIntegrationProvider { get; set; } // Finvu, Perfios, Setu
    public string? ConsentId { get; set; }
    public ICollection<BankTransaction> Transactions { get; set; } = new List<BankTransaction>();
}

public class BankTransaction : TenantEntity
{
    public Guid BankAccountId { get; set; }
    public string? ExternalTransactionId { get; set; }
    public DateTime TransactionDate { get; set; }
    public string TransactionType { get; set; } = string.Empty; // CR, DR
    public decimal Credit => TransactionType == "CR" ? Amount : 0;
    public decimal Debit  => TransactionType == "DR" ? Amount : 0;
    public decimal Amount { get; set; }
    public decimal BalanceAfter { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? Narration { get; set; }
    public string? ChequeNumber { get; set; }
    public string? UTRNumber { get; set; }
    public ReconciliationStatus ReconciliationStatus { get; set; } = ReconciliationStatus.Unreconciled;
    public Guid? LinkedPaymentId { get; set; }
    public Guid? LinkedInvoiceId { get; set; }
    public Guid? LinkedExpenseId { get; set; }
    public string? Category { get; set; } // AI auto-categorized
    public bool IsSuspicious { get; set; } // AI fraud flag
    public string? SuspiciousReason { get; set; }
}

public class Expense : TenantEntity
{
    public string ExpenseNumber { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string SubCategory { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal? GSTAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTime ExpenseDate { get; set; }
    public DateTime Date => ExpenseDate;
    public string? VendorName { get; set; }
    public Guid? VendorId { get; set; }
    public PaymentMode PaymentMode { get; set; }
    public string? ReferenceNumber { get; set; }
    public string? BillNumber { get; set; }
    public bool IsReimbursable { get; set; }
    public string? ReimbursedTo { get; set; }
    public bool IsRecurring { get; set; }
    public string? ReceiptUrl { get; set; }
    public string? Notes { get; set; }
    public bool IsApproved { get; set; }
    public string? ApprovedBy { get; set; }
    public bool IsTaxDeductible { get; set; }
    public Guid? BankTransactionId { get; set; }
}

public class CashFlowForecast : TenantEntity
{
    public DateTime ForecastDate { get; set; }
    public DateTime ForecastPeriodStart { get; set; }
    public DateTime ForecastPeriodEnd { get; set; }
    public string ForecastType { get; set; } = "Weekly"; // Weekly, Monthly, Quarterly
    public decimal OpeningBalance { get; set; }
    public decimal ExpectedInflows { get; set; }
    public decimal ExpectedOutflows { get; set; }
    public decimal ProjectedClosingBalance { get; set; }
    public decimal AIConfidenceScore { get; set; }
    public string? AIInsightNotes { get; set; }
    public ICollection<CashFlowItem> Items { get; set; } = new List<CashFlowItem>();
}

public class CashFlowItem : TenantEntity
{
    public Guid ForecastId { get; set; }
    public DateTime Date { get; set; }
    public string Type { get; set; } = string.Empty; // Inflow, Outflow
    public string Category { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal Probability { get; set; } = 1.0m; // AI confidence
    public bool IsActual { get; set; }
    public decimal? ActualAmount { get; set; }
}

public class LoanApplication : TenantEntity
{
    public string LoanType { get; set; } = string.Empty; // Working Capital, Term Loan, MUDRA, CGTMSE, etc.
    public decimal RequestedAmount { get; set; }
    public decimal? ApprovedAmount { get; set; }
    public string? Purpose { get; set; }
    public int TenureMonths { get; set; }
    public string? LenderName { get; set; }
    public LoanStatus Status { get; set; }
    public decimal? InterestRate { get; set; }
    public decimal? EMIAmount { get; set; }
    public DateTime? DisbursedAt { get; set; }
    public string? LoanAccountNumber { get; set; }
    public string? ApplicationReference { get; set; }
    public decimal AIEligibilityScore { get; set; } // Our AI predicted score
    public string? AIRecommendation { get; set; }
    public ICollection<LoanDocument> Documents { get; set; } = new List<LoanDocument>();
}

public class LoanDocument : TenantEntity
{
    public Guid LoanApplicationId { get; set; }
    public string DocumentType { get; set; } = string.Empty;
    public string FileUrl { get; set; } = string.Empty;
    public bool IsVerified { get; set; }
}
