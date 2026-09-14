using MyErp.Application.Abstractions;
using MyErp.Application.DTOs;

namespace MyErp.Application.Services;

public interface IInventoryService
{
    /// <summary>ERP.md §4.5 / §6：即時庫存列表。Phase 2 的異動明細查詢／手動調整這次先不做。</summary>
    Task<List<InventoryItemDto>> GetInventoryAsync(CancellationToken ct = default);
}

public class InventoryService(IProductRepository productRepository) : IInventoryService
{
    public async Task<List<InventoryItemDto>> GetInventoryAsync(CancellationToken ct = default)
    {
        // lowStock: null 代表不篩選（全部啟用中的商品都列出來，前端自己依 IsLowStock 標記提示）。
        var products = await productRepository.SearchAsync(keyword: null, categoryId: null, lowStock: null, ct);

        return products.Select(p => new InventoryItemDto
        {
            ProductId = p.Id,
            Sku = p.Sku,
            Name = p.Name,
            CategoryName = p.Category?.Name,
            Unit = p.Unit,
            CurrentStock = p.CurrentStock,
            SafetyStock = p.SafetyStock,
            IsLowStock = p.CurrentStock < p.SafetyStock,
        }).ToList();
    }
}
