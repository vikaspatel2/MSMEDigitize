namespace MSMEDigitize.Core.DTOs;

public class GSTSummaryDto
{
    public decimal TotalSales { get; set; }
    public decimal TotalPurchases { get; set; }
    public decimal OutputTax { get; set; }
    public decimal InputTax { get; set; }
    public decimal NetTaxPayable { get; set; }
}

public class GSTR1SummaryDto
{
    public int TotalInvoices { get; set; }
    public decimal TotalTaxableValue { get; set; }
    public decimal TotalTaxAmount { get; set; }
}


public class GSTR3BSummaryDto
{
    public decimal OutwardTaxableSupplies { get; set; }
    public decimal OutwardTax { get; set; }
    public decimal InwardSupplies { get; set; }
    public decimal ITCClaimed { get; set; }
    public decimal NetTaxPayable { get; set; }
}


public class HSNSummaryDto
{
    public string HSNCode { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal TaxRate { get; set; }
}


public class ITCReconciliationDto
{
    public decimal BooksITC { get; set; }
    public decimal GSTR2AITC { get; set; }
    public decimal Difference { get; set; }
    public string Status { get; set; } = string.Empty;
}
