using MyErp.Domain.Common;

namespace MyErp.Domain.Entities;

/// <summary>商品（ERP.md §5.1 Product）。</summary>
public class Product : IAuditable
{
    public int Id { get; set; }

    /// <summary>內部編號，唯一（只在未刪除的商品之間唯一，見 MyErpDbContext 的篩選式唯一索引）。</summary>
    public string Sku { get; set; } = string.Empty;

    /// <summary>條碼，可為空，若有值則唯一（掃碼查詢用；同樣只在未刪除的商品之間唯一）。</summary>
    public string? Barcode { get; set; }

    public string Name { get; set; } = string.Empty;

    public int CategoryId { get; set; }
    public Category? Category { get; set; }

    /// <summary>單位（個/箱/瓶…）。</summary>
    public string Unit { get; set; } = string.Empty;

    /// <summary>參考成本價：採「最近一次進貨價」，每次進貨單成立時由 PurchaseOrderService 更新。</summary>
    public decimal CostPrice { get; set; }

    public decimal SalePrice { get; set; }

    public int SafetyStock { get; set; }

    /// <summary>
    /// 目前庫存快取欄位。ERP.md §5.1 備註：「Product.CurrentStock 快取欄位 + InventoryTransaction 明細表」雙軌，
    /// 查詢即時庫存快，異動明細可稽核、可重算校正。所有異動都要同時寫 InventoryTransaction，不能只改這個欄位。
    /// </summary>
    public int CurrentStock { get; set; }

    /// <summary>常用供應商，非必填。</summary>
    public int? SupplierId { get; set; }
    public Supplier? Supplier { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public string CreatedBy { get; set; } = string.Empty;
    public string UpdatedBy { get; set; } = string.Empty;

    /// <summary>是否已刪除（軟刪除，原本叫 IsActive，這次統一改名成 IsDeleted 並反轉語意）。</summary>
    public bool IsDeleted { get; set; }

    public ICollection<PurchaseOrderItem> PurchaseOrderItems { get; set; } = new List<PurchaseOrderItem>();
    public ICollection<SalesOrderItem> SalesOrderItems { get; set; } = new List<SalesOrderItem>();
    public ICollection<InventoryTransaction> InventoryTransactions { get; set; } = new List<InventoryTransaction>();
}
