using MyErp.Domain.Common;

namespace MyErp.Domain.Entities;

/// <summary>供應商（ERP.md §5.1 Supplier）。</summary>
public class Supplier : IAuditable
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? ContactPerson { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? Note { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public string CreatedBy { get; set; } = string.Empty;
    public string UpdatedBy { get; set; } = string.Empty;

    /// <summary>是否已刪除（軟刪除，原本叫 IsActive，這次統一改名成 IsDeleted 並反轉語意）。</summary>
    public bool IsDeleted { get; set; }

    public ICollection<Product> Products { get; set; } = new List<Product>();
    public ICollection<PurchaseOrder> PurchaseOrders { get; set; } = new List<PurchaseOrder>();
}
