namespace MSMEDigitize.Core.Enums;

public enum MsmeCategory { Micro, Small, Medium }
public enum BusinessType { SoleProprietorship, Partnership, LLP, PrivateLimited, PublicLimited, HUF, Trust, Society, OPC,
    Proprietorship
}
public enum TenantStatus { Trial, Active, Suspended, Cancelled, Churned }
public enum SubscriptionPlanType { Free, Starter, Growth, Professional, Enterprise }
public enum FinancialYearStart { April, January }
public enum TenantRole { Owner, Admin, Accountant, Manager, Staff, ReadOnly }
public enum IntegrationType { Payment, Banking, ECommerce, Accounting, Logistics, SMS, Email, WhatsApp, GST, ICICI, HDFC, Axis, Kotak, Tally, Zoho, QuickBooks, Shopify, Amazon, Flipkart, IndiaMART }

// GST
public enum GSTReturnType { GSTR1, GSTR2A, GSTR2B, GSTR3B, GSTR4, GSTR9, GSTR9C, CMP08 }
public enum GSTReturnStatus { Draft, Filed, Pending, Overdue, NilFiled }
public enum GSTTransactionType { B2B, B2C, Exports, Imports, RCM, ISD, CDNR, CDNUR,
    Purchase
}
public enum SupplyType { Taxable, Exempt, Nil, NonGST }
public enum GSTType { CGST, SGST, IGST, UTGST, Cess }

// Invoice
public enum InvoiceType
{
    TaxInvoice = 0,
    Tax = 0,
    ProformaInvoice = 1,
    Proforma = 1,
    QuotationEstimate = 2,
    Estimate = 2,
    CreditNote = 3,
    DebitNote = 4,
    DeliveryChalan = 5,
    PurchaseOrder = 6,
    EInvoice = 7,
    Recurring = 8
}
public enum InvoiceStatus { Draft, Sent, Viewed, PartiallyPaid, Paid, Overdue, Cancelled, Disputed }
public enum PaymentMode { Cash, Cheque, NEFT, RTGS, IMPS, UPI, Card, EMI, BNPLCredit }
public enum EInvoiceStatus { NotRequired, Pending, Generated, Cancelled, Failed }
public enum EWayBillStatus { NotRequired, Generated, Cancelled, Expired, Pending }

// Inventory
public enum ItemType { Product, Service, Bundle, DigitalGoods }
public enum StockAdjustmentReason { Opening, Purchase, Sale, Damage, Return, Transfer, Audit, Expired, Production }
public enum WarehouseType { Own, ThirdParty, Virtual, FBA, FBF }
public enum UnitOfMeasure { Nos, Kg, Gram, Litre, ML, Meter, SqMeter, CbMeter, Box, Pack, Dozen, Set, Pair, Hour, Day }

// Finance
public enum AccountType { Asset, Liability, Equity, Revenue, Expense, Income }
public enum JournalEntryType { Opening, Sales, Purchase, Payment, Receipt, Contra, Transfer, Adjustment, Depreciation, Accrual }
public enum ReconciliationStatus { Unreconciled, PartiallyReconciled, Reconciled, Exception }
public enum LoanStatus { Applied, UnderReview, Approved, Disbursed, Active, Closed, NPA, Rejected }

// Payroll
public enum EmployeeStatus { Active, OnLeave, Probation, Notice, Terminated, Contract }
public enum PayrollStatus { Draft, Processed, Approved, Disbursed, Locked }
public enum LeaveType { CasualLeave, SickLeave, EarnedLeave, MaternityLeave, PaternityLeave, CompOff, UnpaidLeave }

// Compliance
public enum ComplianceCategory { GST, IncomeTax, PF, ESI, PT, TDS, ROC, FSSAI, MSME, Labour, Environment }
public enum ComplianceStatus { Upcoming, Overdue, Filed, Pending, NotApplicable, Waived }
public enum RiskLevel { Low, Medium, High, Critical }

// AI Features
public enum AIInsightType
{
    CashFlowForecast = 0,
    CashFlow = 0,
    TaxOptimization = 1,
    InventoryOptimization = 2,
    Inventory = 2,
    CustomerChurn = 3,
    FraudAlert = 4,
    PriceOptimization = 5,
    LoanEligibility = 6,
    GrowthOpportunity = 7
}
public enum AIInsightStatus { Active, Dismissed, ActedUpon, Expired }

// Notifications
public enum NotificationChannel { Email, SMS, WhatsApp, PushNotification, InApp }
public enum NotificationType { PaymentReminder, TaxDue, ComplianceDue, LowStock, InvoiceViewed, PaymentReceived, SystemAlert, AIInsight, Announcement }

public enum StockMovementType { In, Out, Adjustment, Transfer, Return, Opening }

public enum PaymentMethod { Cash, Card, UPI, NEFT, RTGS, IMPS, Cheque, BankTransfer, Other }

public enum UserRole { SuperAdmin, Admin, Accountant, Manager, Staff, Viewer, ReadOnly }

public enum AlertType { Info, Warning, Error, Success, Payment, Invoice, Stock, GST, Payroll }

// Alias for SubscriptionPlanType used in entity fields
// SubscriptionPlan enum renamed to SubscriptionPlanTier to avoid ambiguity with SubscriptionPlan entity
public enum SubscriptionPlanTier { Free, Starter, Professional, Enterprise }
// Backward-compat alias
#pragma warning disable CS0618
public enum SubscriptionPlan { Free = 0, Starter = 1, Professional = 2, Enterprise = 3 }
#pragma warning restore CS0618

// Customer classification
public enum CustomerType { Retail, Wholesale, Corporate, Government, Export }

public enum BusinessCategory { Retail, Manufacturing, Services, Trading, Food, Healthcare, Education, Technology, Agriculture, Construction, Textile, Hospitality, Transport, Other }

// Accounting
public enum AccountGroup { Assets, Liabilities, Equity, Revenue, Expenses, CapitalAccount, BankAccounts, CashInHand, Investments, Loans, Duties, Sundry, CurrentAsset, CurrentLiability, DirectExpense, IndirectExpense }
public enum TransactionType { Debit, Credit, Contra, Opening, Closing }

// CRM
public enum InteractionType { Call, Email, Meeting, WhatsApp, SMS, Visit, Demo, Support, Complaint, Followup }

// Sales
public enum SalesOrderStatus { Pending, Confirmed, Processing, Shipped, Delivered, Cancelled, Returned }

// HR additions
public enum AttendanceStatus { Present, Absent, HalfDay, WFH, Leave, Holiday, WeekOff }
public enum PayrollRunStatus { Draft, Processing, Approved, Disbursed, Cancelled }

// Payment / Subscription additions
public enum PaymentStatus { Created, Authorized, Captured, Refunded, Failed, Expired }
public enum RecurringFrequency { Daily, Weekly, Fortnightly, Monthly, Quarterly, HalfYearly, Annually }
public enum BillingCycle { Monthly, Quarterly, HalfYearly, Annually }

// InvoiceType aliases (so both Tax and TaxInvoice work)
// These are kept as an extension in code — use InvoiceType.TaxInvoice, CreditNote etc.
// For shorthand aliases, use in-code: const InvoiceType Tax = InvoiceType.TaxInvoice;