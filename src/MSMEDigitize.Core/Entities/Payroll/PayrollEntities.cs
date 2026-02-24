using MSMEDigitize.Core.Common;
using MSMEDigitize.Core.Enums;
using MSMEDigitize.Core.Entities.AI;
using MSMEDigitize.Core.Entities.Banking;
using MSMEDigitize.Core.Entities.GST;
using MSMEDigitize.Core.Entities.Inventory;
using MSMEDigitize.Core.Entities.Invoicing;
using MSMEDigitize.Core.Entities.Subscriptions;
using MSMEDigitize.Core.Entities.Tenants;

namespace MSMEDigitize.Core.Entities.Payroll;

public class Employee : TenantEntity
{
    public string EmployeeCode { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? FatherName { get; set; }
    public DateTime DateOfBirth { get; set; }
    public string Gender { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? EmergencyContact { get; set; }
    public Address PermanentAddress { get; set; } = new();
    public Address? CurrentAddress { get; set; }
    public Guid? DepartmentGuid { get; set; }
    public string? DepartmentId { get; set; }
    public MSMEDigitize.Core.Entities.Department? Department { get; set; }
    public Guid? DesignationGuid { get; set; }
    public string? DesignationId { get; set; }
    public MSMEDigitize.Core.Entities.Designation? Designation { get; set; }
    public string? ManagerId { get; set; }
    public DateTime JoiningDate { get; set; }
    public DateTime? ConfirmationDate { get; set; }
    public EmployeeStatus Status { get; set; } = EmployeeStatus.Active;
    public bool IsActive => Status != EmployeeStatus.Terminated;
    public string EmploymentType { get; set; } = "Permanent"; // Permanent, Contract, Intern, Part-time
    public string? WorkLocation { get; set; }
    public decimal BasicSalary { get; set; }
    public decimal HRA { get; set; }
    public decimal SpecialAllowance { get; set; }
    public decimal ConveyanceAllowance { get; set; }
    public decimal MedicalAllowance { get; set; }
    public decimal VariablePay { get; set; }
    public decimal GrossSalary { get; set; }
    public decimal CTC { get; set; }
    public string? BankAccountNumber { get; set; }
    public string? IFSC { get; set; }
    public string? BankName { get; set; }
    public string? PAN { get; set; }
    public string? Aadhaar { get; set; } // Masked: XXXX-XXXX-1234
    public string? PFAccountNumber { get; set; }
    public string? UANNumber { get; set; }
    public string? ESINumber { get; set; }
    public bool PFApplicable { get; set; } = true;
    public bool ESIApplicable { get; set; }
    public bool PTApplicable { get; set; }
    public bool TDSApplicable { get; set; }
    public decimal PFContributionPercent { get; set; } = 12;
    public string TaxRegime { get; set; } = "New"; // Old, New
    public ICollection<LeaveBalance> LeaveBalances { get; set; } = new List<LeaveBalance>();
    public ICollection<Document> Documents { get; set; } = new List<Document>();
}

public class Payslip : TenantEntity
{
    public Guid EmployeeId { get; set; }
    public Employee? Employee { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }
    public PayrollStatus Status { get; set; }
    public decimal BasicSalary { get; set; }
    public decimal HRA { get; set; }
    public decimal SpecialAllowance { get; set; }
    public decimal ConveyanceAllowance { get; set; }
    public decimal MedicalAllowance { get; set; }
    public decimal OvertimePay { get; set; }
    public decimal Bonus { get; set; }
    public decimal LeaveTravelAllowance { get; set; }
    public decimal GrossSalary { get; set; }
    public decimal PFEmployee { get; set; }
    public decimal PFEmployer { get; set; }
    public decimal ESIEmployee { get; set; }
    public decimal ESIEmployer { get; set; }
    public decimal ProfessionalTax { get; set; }
    public decimal IncomeTax { get; set; }
    public decimal LoanDeduction { get; set; }
    public decimal OtherDeductions { get; set; }
    public decimal TotalDeductions { get; set; }
    public decimal NetSalary { get; set; }
    public decimal WorkingDays { get; set; }
    public decimal PaidDays { get; set; }
    public decimal LopDays { get; set; }
    public decimal PresentDays { get; set; }
    public bool IsDisbursed { get; set; }
    public DateTime? DisbursedAt { get; set; }
    public string? PayslipUrl { get; set; }
    public string? TransactionRef { get; set; }
}

public class LeaveBalance : TenantEntity
{
    public Guid EmployeeId { get; set; }
    public LeaveType LeaveType { get; set; }
    public int Year { get; set; }
    public decimal Entitled { get; set; }
    public decimal Taken { get; set; }
    public decimal Available { get; set; }
    public decimal CarryForward { get; set; }
}

public class LeaveRequest : TenantEntity
{
    public Guid EmployeeId { get; set; }
    public LeaveType LeaveType { get; set; }
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public decimal TotalDays { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending"; // Pending, Approved, Rejected, Cancelled
    public string? ApprovedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? RejectReason { get; set; }
    public string? Attachment { get; set; }
}

public class Attendance : TenantEntity
{
    public Guid EmployeeId { get; set; }
    public DateTime Date { get; set; }
    public MSMEDigitize.Core.Enums.AttendanceStatus Status { get; set; } = MSMEDigitize.Core.Enums.AttendanceStatus.Present;
    public TimeSpan? CheckIn { get; set; }
    public TimeSpan? CheckOut { get; set; }
    public TimeSpan? WorkHours { get; set; }
    public decimal? OvertimeHours { get; set; }
    public string? Source { get; set; } // Manual, Biometric, Mobile, Geo
    public double? CheckInLatitude { get; set; }
    public double? CheckInLongitude { get; set; }
}

public class Document : TenantEntity
{
    public string DocumentType { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string FileUrl { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public DateTime? ExpiryDate { get; set; }
}

// Payroll run entity (moved from root to avoid namespace conflict)
public class PayrollRun : TenantEntity
{
    public Guid EmployeeId { get; set; }
    public int Month { get; set; }
    public int Year { get; set; }
    public int WorkingDays { get; set; }
    public int PresentDays { get; set; }
    public int AbsentDays { get; set; }
    public int LeaveDays { get; set; }
    public decimal BasicSalary { get; set; }
    public decimal HRA { get; set; }
    public decimal SpecialAllowance { get; set; }
    public decimal OtherAllowances { get; set; }
    public decimal GrossEarnings { get; set; }
    public decimal PFEmployee { get; set; }
    public decimal PFEmployer { get; set; }
    public decimal ESICEmployee { get; set; }
    public decimal ESICEmployer { get; set; }
    public decimal ProfessionalTax { get; set; }
    public decimal TDS { get; set; }
    public decimal OtherDeductions { get; set; }
    public decimal TotalDeductions { get; set; }
    public decimal NetSalary { get; set; }
    public PayrollStatus Status { get; set; } = PayrollStatus.Draft;
    public DateTime? ProcessedAt { get; set; }
    public DateTime? PaidAt { get; set; }
    public string? PaymentReference { get; set; }
    public virtual Employee? Employee { get; set; }
}