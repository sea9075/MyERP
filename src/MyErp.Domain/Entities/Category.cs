namespace MyErp.Domain.Entities;

/// <summary>商品分類（ERP.md §5.1 Category）。</summary>
public class Category
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    public ICollection<Product> Products { get; set; } = new List<Product>();
}
