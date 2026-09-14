using MyErp.Domain.Enums;

namespace MyErp.Domain.Entities;

/// <summary>進貨單（ERP.md §5.1 PurchaseOrder）。</summary>
public class PurchaseOrder
{
    public int Id { get; set; }

    /// <summary>單號，格式如 PI-20260908-001（見 PurchaseOrderService 產生規則）。</summary>
    public string OrderNo { get; set; } = string.Empty;

    public int SupplierId { get; set; }
    public Supplier? Supplier { get; set; }

    public DateTime OrderDate { get; set; }

    public OrderStatus Status { get; set; } = OrderStatus.Normal;

    public string? Note { get; set; }

    public int CreatedByUserId { get; set; }
    public User? CreatedByUser { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<PurchaseOrderItem> Items { get; set; } = new List<PurchaseOrderItem>();
}
