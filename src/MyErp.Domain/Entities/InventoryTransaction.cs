using MyErp.Domain.Enums;

namespace MyErp.Domain.Entities;

/// <summary>
/// 庫存異動紀錄（ERP.md §5.1 InventoryTransaction）。稽核用，所有庫存變化都要留痕，
/// 不可只更新 Product.CurrentStock 而不寫這張表。
/// </summary>
public class InventoryTransaction
{
    public int Id { get; set; }

    public int ProductId { get; set; }
    public Product? Product { get; set; }

    public InventoryChangeType ChangeType { get; set; }

    /// <summary>正數＝增加，負數＝減少。</summary>
    public int QuantityChange { get; set; }

    /// <summary>異動後庫存快照。</summary>
    public int StockAfter { get; set; }

    /// <summary>來源單據類型："PurchaseOrder" / "SalesOrder" / "Manual"。</summary>
    public string RefTable { get; set; } = string.Empty;

    /// <summary>來源單據 Id，手動調整（Phase 2）時可為 null。</summary>
    public int? RefId { get; set; }

    /// <summary>手動調整時填寫原因（Phase 2）。</summary>
    public string? Reason { get; set; }

    public int CreatedByUserId { get; set; }
    public User? CreatedByUser { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
