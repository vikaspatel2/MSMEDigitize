using MSMEDigitize.Core.Common;
using MSMEDigitize.Core.Enums;
using MSMEDigitize.Core.Entities.Banking;
using MSMEDigitize.Core.Entities.GST;
using MSMEDigitize.Core.Entities.Inventory;
using MSMEDigitize.Core.Entities.Invoicing;
using MSMEDigitize.Core.Entities.Payroll;
using MSMEDigitize.Core.Entities.Subscriptions;
using MSMEDigitize.Core.Entities.Tenants;

namespace MSMEDigitize.Core.Entities.AI;

public class AIInsight : TenantEntity
{
    public AIInsightType InsightType { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string? DetailedAnalysis { get; set; }
    public RiskLevel RiskLevel { get; set; }
    public decimal? PotentialSaving { get; set; }
    public decimal? PotentialRevenue { get; set; }
    public string? ActionRecommended { get; set; }
    public string? ActionUrl { get; set; } // deep link within app
    public decimal ConfidenceScore { get; set; }
    public AIInsightStatus Status { get; set; } = AIInsightStatus.Active;
    public DateTime? ExpiresAt { get; set; }
    public string? ModelVersion { get; set; }
    public string? RawDataJson { get; set; }
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public string? Description { get; set; }
    public string? Recommendation { get; set; }
    public decimal? PotentialImpact { get; set; }
    public string? ImpactUnit { get; set; }
    public decimal Priority { get; set; } = 5;
    public bool IsRead { get; set; }
    public DateTime? ReadAt { get; set; }
    public DateTime? DismissedAt { get; set; }
}

public class TaxOptimizationSuggestion : TenantEntity
{
    public string Category { get; set; } = string.Empty; // Section 80C, HRA, etc.
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal EstimatedTaxSaving { get; set; }
    public int FinancialYear { get; set; }
    public bool IsActedUpon { get; set; }
    public string? ActionDetails { get; set; }
    public decimal Priority { get; set; } // 1-10
}

public class CustomerChurnPrediction : TenantEntity
{
    public Guid CustomerId { get; set; }
    public decimal ChurnProbability { get; set; } // 0-1
    public string ChurnRisk { get; set; } = string.Empty; // Low, Medium, High
    public decimal DaysSinceLastTransaction { get; set; }
    public decimal LifetimeValue { get; set; }
    public string? RetentionStrategy { get; set; }
    public bool IsEngaged { get; set; }
    public DateTime PredictionDate { get; set; }
    public DateTime PredictedAt { get; set; } = DateTime.UtcNow;
}

public class FraudAlert : TenantEntity
{
    public string AlertType { get; set; } = string.Empty; // Duplicate Invoice, Unusual Pattern, Fake GSTIN, etc.
    public string Description { get; set; } = string.Empty;
    public RiskLevel RiskLevel { get; set; }
    public Guid? ReferenceId { get; set; }
    public string? ReferenceType { get; set; }
    public string Severity { get; set; } = "Medium";
    public DateTime DetectedAt { get; set; } = DateTime.UtcNow;
    public string? RelatedEntityType { get; set; }
    public Guid? RelatedEntityId { get; set; }
    public decimal? AmountAtRisk { get; set; }
    public string Status { get; set; } = "Open"; // Open, Investigating, Resolved, FalsePositive
    public string? Resolution { get; set; }
    public DateTime? ResolvedAt { get; set; }
}

public class PriceOptimization : TenantEntity
{
    public Guid ProductId { get; set; }
    public decimal CurrentPrice { get; set; }
    public decimal SuggestedPrice { get; set; }
    public string Reasoning { get; set; } = string.Empty;
    public decimal? EstimatedRevenueImpact { get; set; }
    public decimal? ElasticityScore { get; set; }
    public string CompetitorPriceRange { get; set; } = string.Empty;
    public bool IsAccepted { get; set; }
    public DateTime ValidUntil { get; set; }
}

public class InventoryOptimization : TenantEntity
{
    public Guid ProductId { get; set; }
    public string OptimizationType { get; set; } = string.Empty; // Overstock, Stockout, DeadStock, ReorderOptimize
    public string Description { get; set; } = string.Empty;
    public decimal CurrentStock { get; set; }
    public decimal OptimalStock { get; set; }
    public decimal? SuggestedOrderQuantity { get; set; }
    public decimal? EstimatedSaving { get; set; }
    public decimal? DemandForecast30Days { get; set; }
    public decimal ConfidenceScore { get; set; }
    public bool IsActedUpon { get; set; }
}
