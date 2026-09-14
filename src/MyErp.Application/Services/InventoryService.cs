using MyErp.Application.Abstractions;
using MyErp.Application.Common;
using MyErp.Application.DTOs;
using MyErp.Domain.Entities;
using MyErp.Domain.Enums;

namespace MyErp.Application.Services;

public interface IInventoryService
{
    /// <summary>ERP.md §4.5 / §6：即時庫存列表。</summary>
    Task<List<InventoryItemDto>> GetInventoryAsync(CancellationToken ct = default);

    /// <summary>ERP.md §8 Phase 2 項目 9：GET /api/inventory/{productId}/transactions 庫存異動明細查詢。</summary>
    Task<List<InventoryTransactionDto>> GetTransactionsAsync(int productId, CancellationToken ct = default);

    /// <summary>
    /// ERP.md §8 Phase 2 項目 9：手動盤點調整。輸入的是「調整量」（正負皆可，使用者決定採用此方式），
    /// 系統直接用這個值改庫存並留下 InventoryTransaction 稽核紀錄，調整後庫存不可小於 0。
    /// </summary>
    Task<InventoryItemDto> AdjustAsync(AdjustInventoryRequest request, int currentUserId, string currentUsername, CancellationToken ct = default);
}

public class InventoryService(
    IProductRepository productRepository,
    IInventoryTransactionRepository inventoryTransactionRepository,
    IUnitOfWork unitOfWork) : IInventoryService
{
    public async Task<List<InventoryItemDto>> GetInventoryAsync(CancellationToken ct = default)
    {
        // lowStock: null 代表不篩選（全部啟用中的商品都列出來，前端自己依 IsLowStock 標記提示）。
        // includeDeleted: false，庫存列表只列出未軟刪除的商品。
        var products = await productRepository.SearchAsync(keyword: null, categoryId: null, lowStock: null, includeDeleted: false, ct);

        return products.Select(ToInventoryItemDto).ToList();
    }

    public async Task<List<InventoryTransactionDto>> GetTransactionsAsync(int productId, CancellationToken ct = default)
    {
        // 商品不存在的話先報清楚的錯誤，而不是讓前端拿到一個看起來正常、但其實是空的清單。
        _ = await productRepository.GetByIdAsync(productId, ct)
            ?? throw new BusinessRuleException($"找不到商品 (Id={productId})。");

        var transactions = await inventoryTransactionRepository.GetByProductIdAsync(productId, ct);

        return transactions.Select(t => new InventoryTransactionDto
        {
            Id = t.Id,
            ProductId = t.ProductId,
            ChangeType = t.ChangeType.ToString(),
            QuantityChange = t.QuantityChange,
            StockAfter = t.StockAfter,
            RefTable = t.RefTable,
            RefId = t.RefId,
            Reason = t.Reason,
            CreatedByUserId = t.CreatedByUserId,
            CreatedByUsername = t.CreatedByUser?.Username,
            CreatedAt = t.CreatedAt,
        }).ToList();
    }

    public async Task<InventoryItemDto> AdjustAsync(AdjustInventoryRequest request, int currentUserId, string currentUsername, CancellationToken ct = default)
    {
        if (request.AdjustmentQuantity == 0)
        {
            throw new BusinessRuleException("調整量不能是 0。");
        }

        var product = await productRepository.GetByIdAsync(request.ProductId, ct)
            ?? throw new BusinessRuleException($"找不到商品 (Id={request.ProductId})。");

        var newStock = product.CurrentStock + request.AdjustmentQuantity;
        if (newStock < 0)
        {
            throw new BusinessRuleException(
                $"調整後庫存不能小於 0（目前庫存 {product.CurrentStock}，調整量 {request.AdjustmentQuantity}）。");
        }

        product.CurrentStock = newStock;
        product.TouchUpdated(currentUsername);

        inventoryTransactionRepository.Add(new InventoryTransaction
        {
            ProductId = product.Id,
            ChangeType = InventoryChangeType.ManualAdjustment,
            QuantityChange = request.AdjustmentQuantity,
            StockAfter = newStock,
            RefTable = "Manual",
            RefId = null,
            Reason = request.Reason,
            CreatedByUserId = currentUserId,
            CreatedAt = DateTime.UtcNow,
        });

        // 只有「更新一筆 Product ＋ 新增一筆 InventoryTransaction」，一次 SaveChanges 就是原子操作，
        // 不需要像進貨/出貨單那樣用 ExecuteInTransactionAsync（那是因為需要分兩階段存檔才要包交易）。
        await unitOfWork.SaveChangesAsync(ct);

        return ToInventoryItemDto(product);
    }

    private static InventoryItemDto ToInventoryItemDto(Product p) => new()
    {
        ProductId = p.Id,
        Sku = p.Sku,
        Name = p.Name,
        CategoryName = p.Category?.Name,
        Unit = p.Unit,
        CurrentStock = p.CurrentStock,
        SafetyStock = p.SafetyStock,
        IsLowStock = p.CurrentStock < p.SafetyStock,
    };
}
