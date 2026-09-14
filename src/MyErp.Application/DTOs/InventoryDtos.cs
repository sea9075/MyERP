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
