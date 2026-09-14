using MyErp.Domain.Common;

namespace MyErp.Domain.Entities;

/// <summary>客戶（ERP.md §5.1 Customer，選填；完整 CRUD API 屬於 ERP.md §8 Phase 2 項目 11）。</summary>
public class Customer : IAuditable
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Note { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public string CreatedBy { get; set; } = string.Empty;
    public string UpdatedBy { get; set; } = string.Empty;

    /// <summary>是否已刪除（軟刪除）。刪除前會檢查是否還有出貨單引用，見 CustomerService.DeleteAsync。</summary>
    public bool IsDeleted { get; set; }

    public ICollection<SalesOrder> SalesOrders { get; set; } = new List<SalesOrder>();
}
