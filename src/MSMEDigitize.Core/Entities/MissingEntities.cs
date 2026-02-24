// MissingEntities.cs — entities referenced across the codebase
using MSMEDigitize.Core.Common;
using MSMEDigitize.Core.Enums;

namespace MSMEDigitize.Core.Entities;

public class Department : TenantEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
}

public class Designation : TenantEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
    public string? Description { get; set; }
    public Guid? DepartmentId { get; set; }
    public bool IsActive { get; set; } = true;
}

public class RecurringInvoiceConfig : TenantEntity
{
    public string Name { get; set; } = string.Empty;
    public Guid CustomerId { get; set; }
    public MSMEDigitize.Core.Entities.Invoicing.Customer? Customer { get; set; }
    public RecurringFrequency Frequency { get; set; }
    public int? DayOfMonth { get; set; }
    public DateTime NextRunDate { get; set; }
    public DateTime NextInvoiceDate { get; set; }  // alias used in InvoiceService
    public DateTime? EndDate { get; set; }
    public bool IsActive { get; set; } = true;
    public bool AutoSend { get; set; } = false;
    public string? TemplateData { get; set; }
}