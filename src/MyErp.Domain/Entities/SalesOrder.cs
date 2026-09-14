using MyErp.Domain.Enums;

namespace MyErp.Domain.Entities;

/// <summary>出貨/銷售單（ERP.md §5.1 SalesOrder）。</summary>
public class SalesOrder
{
    public int Id { get; set; }

    /// <summary>單號，格式如 SO-20260908-001。</summary>
    public string OrderNo { get; set; } = string.Empty;

    /// <summary>客戶，可留空＝一般散客。</summary>
    public int? CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public DateTime OrderDate { get; set; }

    public OrderStatus Status { get; set; } = OrderStatus.Normal;

    public string? Note { get; set; }

    public int CreatedByUserId { get; set; }
    public User? CreatedByUser { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<SalesOrderItem> Items { get; set; } = new List<SalesOrderItem>();
}
