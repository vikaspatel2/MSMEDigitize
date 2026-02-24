using MSMEDigitize.Core.Common;
using MSMEDigitize.Core.Enums;
using MSMEDigitize.Core.Entities.AI;
using MSMEDigitize.Core.Entities.Banking;
using MSMEDigitize.Core.Entities.GST;
using MSMEDigitize.Core.Entities.Invoicing;
using MSMEDigitize.Core.Entities.Payroll;
using MSMEDigitize.Core.Entities.Subscriptions;
using MSMEDigitize.Core.Entities.Tenants;

namespace MSMEDigitize.Core.Entities.Inventory;

public class Product : TenantEntity
{
    public string Code { get; set; } = string.Empty;
    public string SKU { get; set; } = string.Empty;
    public string Barcode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ItemType ItemType { get; set; }
    public string? CategoryId { get; set; }
    public Guid? ProductCategoryId { get; set; }
    public ProductCategory? Category { get; set; }  // navigation property
    public string? BrandId { get; set; }
    public string HSNCode { get; set; } = string.Empty;
    public decimal GSTRate { get; set; }
    public decimal CessRate { get; set; }
    public UnitOfMeasure Unit { get; set; }
    public decimal PurchasePrice { get; set; }
    public decimal MRP { get; set; }
    public decimal SellingPrice { get; set; }
    public decimal SalePrice => SellingPrice;
    public decimal ReorderPoint => MinStockLevel;
    public decimal WholesalePrice { get; set; }
    public decimal? MinSellingPrice { get; set; } // AI auto-pricing floor
    public bool HasVariants { get; set; }
    public bool TrackInventory { get; set; } = true;
    public decimal MinStockLevel { get; set; } // reorder point
    public decimal MaxStockLevel { get; set; }
    public decimal ReorderQuantity { get; set; }
    public decimal CurrentStock { get; set; }
    public decimal? WeightKg { get; set; }
    public string? ImageUrl { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsTaxable { get; set; } = true;
    public string? Location { get; set; } // bin/rack location
    public string? ManufacturerName { get; set; }
    public string? CountryOfOrigin { get; set; } = "India";
    public bool HasBatchTracking { get; set; }
    public bool HasSerialTracking { get; set; }
    public bool HasExpiryTracking { get; set; }
    public string? Tags { get; set; }
    public ICollection<ProductVariant> Variants { get; set; } = new List<ProductVariant>();
    public ICollection<StockLedger> StockLedger { get; set; } = new List<StockLedger>();
}

public class ProductVariant : TenantEntity
{
    public Guid ProductId { get; set; }
    public string VariantName { get; set; } = string.Empty;
    public string? SKU { get; set; }
    public string? Barcode { get; set; }
    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public Dictionary<string, string> Attributes { get; set; } = new(); // Color: Red, Size: L
    public string AttributesJson { get => System.Text.Json.JsonSerializer.Serialize(Attributes); set { try { Attributes = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(value ?? "{}") ?? new(); } catch { Attributes = new(); } } }
    public decimal PriceDifference { get; set; }
    public decimal CurrentStock { get; set; }
    public string? ImageUrl { get; set; }
    public bool IsActive { get; set; } = true;
}

public class Warehouse : TenantEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public WarehouseType Type { get; set; }
    public Address Address { get; set; } = new();
    public string? ManagerName { get; set; }
    public string? ManagerPhone { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsPrimary { get; set; }
    public string? GSTIN { get; set; } // for multi-state warehouses
}

public class StockLedger : TenantEntity
{
    public Guid ProductId { get; set; }
    public Guid? WarehouseId { get; set; }
    public StockAdjustmentReason Reason { get; set; }
    public string TransactionType { get; set; } = string.Empty; // IN, OUT
    public decimal Quantity { get; set; }
    public decimal UnitCost { get; set; }
    public decimal TotalCost { get; set; }
    public decimal StockAfter { get; set; }
    public string? ReferenceNumber { get; set; } // Invoice/PO/Transfer number
    public string? BatchNumber { get; set; }
    public string? SerialNumber { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public string? Notes { get; set; }
}

public class StockTransfer : TenantEntity
{
    public string TransferNumber { get; set; } = string.Empty;
    public Guid FromWarehouseId { get; set; }
    public Guid ToWarehouseId { get; set; }
    public DateTime TransferDate { get; set; }
    public string Status { get; set; } = "Draft"; // Draft, InTransit, Received, Cancelled
    public string? Notes { get; set; }
    public ICollection<StockTransferItem> Items { get; set; } = new List<StockTransferItem>();
}

public class StockTransferItem : TenantEntity
{
    public Guid TransferId { get; set; }
    public Guid ProductId { get; set; }
    public decimal Quantity { get; set; }
    public decimal ReceivedQuantity { get; set; }
}

public class BatchSerial : TenantEntity
{
    public Guid ProductId { get; set; }
    public string? BatchNumber { get; set; }
    public string? SerialNumber { get; set; }
    public DateTime? ManufacturingDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public decimal Quantity { get; set; }
    public decimal CostPrice { get; set; }
    public Guid? WarehouseId { get; set; }
    public bool IsAvailable { get; set; } = true;
}

public class ProductCategory : MSMEDigitize.Core.Common.TenantEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ParentId { get; set; }
    public bool IsActive { get; set; } = true;
}