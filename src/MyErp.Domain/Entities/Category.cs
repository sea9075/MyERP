using MyErp.Domain.Common;

namespace MyErp.Domain.Entities;

/// <summary>商品分類（ERP.md §5.1 Category）。</summary>
public class Category : IAuditable
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public string CreatedBy { get; set; } = string.Empty;
    public string UpdatedBy { get; set; } = string.Empty;

    /// <summary>是否已刪除（軟刪除）。刪除前會檢查底下是否還有商品在用，見 CategoryService.DeleteAsync。</summary>
    public bool IsDeleted { get; set; }

    public ICollection<Product> Products { get; set; } = new List<Product>();
}
