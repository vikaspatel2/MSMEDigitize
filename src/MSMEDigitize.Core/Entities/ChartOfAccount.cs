using MSMEDigitize.Core.Common;
using MSMEDigitize.Core.Enums;

namespace MSMEDigitize.Core.Entities;

public class ChartOfAccount : BaseEntity
{
    public Guid TenantId { get; set; }
    public string AccountCode { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public AccountType AccountType { get; set; }
    public AccountGroup AccountGroup { get; set; }
    public Guid? ParentAccountId { get; set; }
    public bool IsSystemAccount { get; set; } = false;
    public bool IsActive { get; set; } = true;
    public decimal OpeningBalance { get; set; } = 0;
    public decimal CurrentBalance { get; set; } = 0;

    public ChartOfAccount? ParentAccount { get; set; }
    public ICollection<ChartOfAccount> SubAccounts { get; set; } = new List<ChartOfAccount>();
    public ICollection<LedgerEntry> LedgerEntries { get; set; } = new List<LedgerEntry>();
}