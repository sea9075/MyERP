using System.ComponentModel.DataAnnotations;

namespace MyErp.Application.DTOs;

/// <summary>對應 ERP.md §4.5 / §6：GET /api/inventory 即時庫存列表。</summary>
public class InventoryItemDto
{
    public int ProductId { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? CategoryName { get; set; }
    public string Unit { get; set; } = string.Empty;
    public int CurrentStock { get; set; }
    public int SafetyStock { get; set; }
    public bool IsLowStock { get; set; }
}

/// <summary>對應 ERP.md §6：GET /api/inventory/{productId}/transactions 庫存異動明細（Phase 2）。</summary>
public class InventoryTransactionDto
{
    public int Id { get; set; }
    public int ProductId { get; set; }

    /// <summary>"Purchase" / "Sale" / "ManualAdjustment" / "PurchaseVoid" / "SaleVoid"。</summary>
    public string ChangeType { get; set; } = string.Empty;

    public int QuantityChange { get; set; }
    public int StockAfter { get; set; }
    public string RefTable { get; set; } = string.Empty;
    public int? RefId { get; set; }
    public string? Reason { get; set; }
    public int CreatedByUserId { get; set; }
    public string? CreatedByUsername { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>對應 ERP.md §6：POST /api/inventory/adjust 手動盤點調整（Phase 2）。</summary>
public class AdjustInventoryRequest
{
    [Required]
    public int ProductId { get; set; }

    /// <summary>調整量，正數＝盤盈（增加），負數＝盤損（減少）。不可為 0。</summary>
    [Required]
    public int AdjustmentQuantity { get; set; }

    [Required, StringLength(200, MinimumLength = 1, ErrorMessage = "手動調整必須填寫原因")]
    public string Reason { get; set; } = string.Empty;
}
