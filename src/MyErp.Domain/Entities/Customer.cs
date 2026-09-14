namespace MyErp.Domain.Entities;

/// <summary>
/// 客戶（ERP.md §5.1 Customer，選填）。
/// Phase 1 只需要這張表存在，讓 SalesOrder.CustomerId 這個可為空的 FK 有地方指，
/// 完整的客戶管理 CRUD API 屬於 ERP.md §8 Phase 2 項目 11，這次不建立對應 Controller。
/// </summary>
public class Customer
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Note { get; set; }

    public ICollection<SalesOrder> SalesOrders { get; set; } = new List<SalesOrder>();
}
