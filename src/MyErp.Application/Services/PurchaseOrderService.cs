using MyErp.Application.Abstractions;
using MyErp.Application.Common;
using MyErp.Application.DTOs;
using MyErp.Domain.Entities;
using MyErp.Domain.Enums;

namespace MyErp.Application.Services;

public interface IPurchaseOrderService
{
    Task<List<PurchaseOrderDto>> SearchAsync(DateTime? dateFrom, DateTime? dateTo, int? supplierId, CancellationToken ct = default);
    Task<PurchaseOrderDto> GetByIdAsync(int id, CancellationToken ct = default);

    /// <summary>
    /// 建立進貨單並自動加庫存（ERP.md §4.3）。整個流程（寫入單據明細＋更新庫存＋寫入異動紀錄）
    /// 都包在同一個資料庫交易內，符合 §9「庫存異動仍建議在單一交易內完成」的要求。
    /// </summary>
    Task<PurchaseOrderDto> CreateAsync(CreatePurchaseOrderRequest request, int currentUserId, CancellationToken ct = default);
}

public class PurchaseOrderService(
    IPurchaseOrderRepository purchaseOrderRepository,
    IProductRepository productRepository,
    ISupplierRepository supplierRepository,
    IInventoryTransactionRepository inventoryTransactionRepository,
    IUnitOfWork unitOfWork) : IPurchaseOrderService
{
    public async Task<List<PurchaseOrderDto>> SearchAsync(DateTime? dateFrom, DateTime? dateTo, int? supplierId, CancellationToken ct = default)
    {
        var orders = await purchaseOrderRepository.SearchAsync(dateFrom, dateTo, supplierId, ct);
        return orders.Select(ToDto).ToList();
    }

    public async Task<PurchaseOrderDto> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var order = await purchaseOrderRepository.GetByIdAsync(id, ct)
            ?? throw new BusinessRuleException($"找不到進貨單 (Id={id})。");
        return ToDto(order);
    }

    public async Task<PurchaseOrderDto> CreateAsync(CreatePurchaseOrderRequest request, int currentUserId, CancellationToken ct = default)
    {
        if (!await supplierRepository.ExistsAsync(request.SupplierId, ct))
        {
            throw new BusinessRuleException($"找不到供應商 (Id={request.SupplierId})。");
        }

        PurchaseOrder order = new()
        {
            SupplierId = request.SupplierId,
            OrderDate = request.OrderDate ?? DateTime.UtcNow,
            Status = OrderStatus.Normal,
            Note = request.Note,
            CreatedByUserId = currentUserId,
            CreatedAt = DateTime.UtcNow,
        };

        // 先把每個商品實際的異動後庫存記下來，等單號／單身都存檔、拿到 order.Id 後，
        // 才能把 InventoryTransaction.RefId 補上，兩者需要分兩次 SaveChanges，但都在同一個交易範圍內。
        var stockAfterByProductId = new Dictionary<int, int>();

        await unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            order.OrderNo = await GenerateOrderNoAsync("PI", ct);

            foreach (var itemRequest in request.Items)
            {
                var product = await productRepository.GetByIdAsync(itemRequest.ProductId, ct)
                    ?? throw new BusinessRuleException($"找不到商品 (Id={itemRequest.ProductId})。");

                order.Items.Add(new PurchaseOrderItem
                {
                    ProductId = product.Id,
                    Quantity = itemRequest.Quantity,
                    UnitPrice = itemRequest.UnitPrice,
                    Subtotal = itemRequest.UnitPrice * itemRequest.Quantity,
                });

                // 庫存增加；CostPrice 依 ERP.md §5.1 說明採「最近一次進貨價」。
                product.CurrentStock += itemRequest.Quantity;
                product.CostPrice = itemRequest.UnitPrice;
                product.UpdatedAt = DateTime.UtcNow;

                stockAfterByProductId[product.Id] = product.CurrentStock;
            }

            purchaseOrderRepository.Add(order);
            await unitOfWork.SaveChangesAsync(ct); // 這行之後 order.Id 才會有值

            foreach (var item in order.Items)
            {
                inventoryTransactionRepository.Add(new InventoryTransaction
                {
                    ProductId = item.ProductId,
                    ChangeType = InventoryChangeType.Purchase,
                    QuantityChange = item.Quantity,
                    StockAfter = stockAfterByProductId[item.ProductId],
                    RefTable = nameof(PurchaseOrder),
                    RefId = order.Id,
                    CreatedByUserId = currentUserId,
                    CreatedAt = DateTime.UtcNow,
                });
            }

            await unitOfWork.SaveChangesAsync(ct);
        }, ct);

        // 重新讀取一次，把 Supplier / Product 的名稱一起帶出來，回傳完整的 DTO。
        var saved = await purchaseOrderRepository.GetByIdAsync(order.Id, ct)
            ?? throw new InvalidOperationException("進貨單儲存後應該要能查得到，這裡不應該發生。");
        return ToDto(saved);
    }

    private async Task<string> GenerateOrderNoAsync(string prefix, CancellationToken ct)
    {
        // 格式如 PI-20260908-001，流水號＝當天已有的張數＋1。
        // 注意：這是簡化版做法，1~5 人使用的低併發情境下足夠，
        // 但嚴格來說仍有極小機率在「幾乎同一毫秒」送出兩張單時撞號，未來要做到完全防呆可以改用資料庫序號表。
        var datePrefix = DateTime.UtcNow.ToString("yyyyMMdd");
        var countToday = await purchaseOrderRepository.CountByDatePrefixAsync(datePrefix, ct);
        return $"{prefix}-{datePrefix}-{(countToday + 1):D3}";
    }

    private static PurchaseOrderDto ToDto(PurchaseOrder order) => new()
    {
        Id = order.Id,
        OrderNo = order.OrderNo,
        SupplierId = order.SupplierId,
        SupplierName = order.Supplier?.Name,
        OrderDate = order.OrderDate,
        Status = order.Status.ToString(),
        Note = order.Note,
        TotalAmount = order.Items.Sum(i => i.Subtotal),
        Items = order.Items.Select(i => new PurchaseOrderItemDto
        {
            ProductId = i.ProductId,
            ProductName = i.Product?.Name,
            Quantity = i.Quantity,
            UnitPrice = i.UnitPrice,
            Subtotal = i.Subtotal,
        }).ToList(),
    };
}
