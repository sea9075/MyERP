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
    /// currentUserId 給 InventoryTransaction.CreatedByUserId（那張表維持原本的 int 外鍵設計，
    /// 沒有套用這次的稽核欄位改動）；currentUsername 給 PurchaseOrder 本身新增的 CreatedBy/UpdatedBy。
    /// </summary>
    Task<PurchaseOrderDto> CreateAsync(CreatePurchaseOrderRequest request, int currentUserId, string currentUsername, CancellationToken ct = default);

    /// <summary>
    /// 作廢進貨單並把當初加的庫存扣回去（ERP.md §8 Phase 2 項目 8）。
    /// 如果扣回去會讓某個商品的庫存變成負的（代表這批貨已經被後續出貨單賣掉一部分），
    /// 整張作廢操作失敗（拋出 BusinessRuleException，Controller 轉成 400），不允許出現負庫存
    /// ——這點跟建立出貨單時「庫存不足就整張失敗」的規則一致（使用者決定採用此做法）。
    /// 作廢本身也算一次異動，會順便更新 UpdatedAt/UpdatedBy。
    /// </summary>
    Task<PurchaseOrderDto> VoidAsync(int id, int currentUserId, string currentUsername, CancellationToken ct = default);
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

    public async Task<PurchaseOrderDto> CreateAsync(CreatePurchaseOrderRequest request, int currentUserId, string currentUsername, CancellationToken ct = default)
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
            Note = request.Note.TrimOrNull(),
        };
        order.InitializeAudit(currentUsername);

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
                product.TouchUpdated(currentUsername);

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

    public async Task<PurchaseOrderDto> VoidAsync(int id, int currentUserId, string currentUsername, CancellationToken ct = default)
    {
        var order = await purchaseOrderRepository.GetByIdAsync(id, ct)
            ?? throw new BusinessRuleException($"找不到進貨單 (Id={id})。");

        if (order.Status == OrderStatus.Voided)
        {
            throw new BusinessRuleException($"進貨單 {order.OrderNo} 已經作廢過了，不能重複作廢。");
        }

        // 同一個商品可能在這張單裡出現在好幾筆明細，要先加總，再一次檢查庫存夠不夠扣，
        // 避免逐筆檢查時因為順序不同而誤判（跟 SalesOrderService.CreateAsync 的檢查方式一致）。
        var voidQuantityByProductId = order.Items
            .GroupBy(i => i.ProductId)
            .ToDictionary(g => g.Key, g => g.Sum(i => i.Quantity));

        var productsById = new Dictionary<int, Product>();
        foreach (var productId in voidQuantityByProductId.Keys)
        {
            var product = await productRepository.GetByIdAsync(productId, ct)
                ?? throw new BusinessRuleException($"找不到商品 (Id={productId})。");
            productsById[productId] = product;
        }

        foreach (var (productId, voidQuantity) in voidQuantityByProductId)
        {
            var product = productsById[productId];
            if (product.CurrentStock < voidQuantity)
            {
                throw new BusinessRuleException(
                    $"無法作廢：商品「{product.Name}」目前庫存（{product.CurrentStock}）不足以扣回這張進貨單加入的數量" +
                    $"（{voidQuantity}），可能是這批貨已經被後續的出貨單賣掉一部分。");
            }
        }

        var stockAfterByProductId = new Dictionary<int, int>();
        foreach (var (productId, voidQuantity) in voidQuantityByProductId)
        {
            var product = productsById[productId];
            product.CurrentStock -= voidQuantity;
            product.TouchUpdated(currentUsername);
            stockAfterByProductId[productId] = product.CurrentStock;
        }

        order.Status = OrderStatus.Voided;
        order.TouchUpdated(currentUsername);

        foreach (var (productId, voidQuantity) in voidQuantityByProductId)
        {
            inventoryTransactionRepository.Add(new InventoryTransaction
            {
                ProductId = productId,
                ChangeType = InventoryChangeType.PurchaseVoid,
                QuantityChange = -voidQuantity,
                StockAfter = stockAfterByProductId[productId],
                RefTable = nameof(PurchaseOrder),
                RefId = order.Id,
                CreatedByUserId = currentUserId,
                CreatedAt = DateTime.UtcNow,
            });
        }

        // order.Id 已經存在（這是既有單據），商品庫存、單據狀態、異動紀錄一次 SaveChanges 就能
        // 一起提交，不需要像 CreateAsync 那樣分兩階段存檔，EF Core 本身就會把這些變更包在同一個交易裡。
        await unitOfWork.SaveChangesAsync(ct);

        return ToDto(order);
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
        CreatedAt = order.CreatedAt,
        UpdatedAt = order.UpdatedAt,
        CreatedBy = order.CreatedBy,
        UpdatedBy = order.UpdatedBy,
    };
}
