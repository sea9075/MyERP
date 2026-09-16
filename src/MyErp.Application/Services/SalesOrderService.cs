using Microsoft.Extensions.Logging;
using MyErp.Application.Abstractions;
using MyErp.Application.Common;
using MyErp.Application.DTOs;
using MyErp.Domain.Entities;
using MyErp.Domain.Enums;

namespace MyErp.Application.Services;

public interface ISalesOrderService
{
    Task<List<SalesOrderDto>> SearchAsync(DateTime? dateFrom, DateTime? dateTo, int? customerId, CancellationToken ct = default);
    Task<SalesOrderDto> GetByIdAsync(int id, CancellationToken ct = default);

    /// <summary>
    /// 建立出貨單並自動扣庫存（ERP.md §4.4）。庫存不足時整張單都不會成立（拋出
    /// BusinessRuleException，Controller 轉成 400），符合「預設不允許負庫存」的規則。
    /// currentUserId 給 InventoryTransaction.CreatedByUserId（那張表維持原本的 int 外鍵設計，
    /// 沒有套用這次的稽核欄位改動）；currentUsername 給 SalesOrder 本身新增的 CreatedBy/UpdatedBy。
    /// </summary>
    Task<SalesOrderDto> CreateAsync(CreateSalesOrderRequest request, int currentUserId, string currentUsername, CancellationToken ct = default);

    /// <summary>
    /// 作廢出貨單並把當初扣掉的庫存加回去（ERP.md §8 Phase 2 項目 8）。
    /// 加回庫存不會有變負數的疑慮，所以不像 PurchaseOrder 作廢那樣需要先檢查庫存夠不夠。
    /// 作廢本身也算一次異動，會順便更新 UpdatedAt/UpdatedBy。
    /// </summary>
    Task<SalesOrderDto> VoidAsync(int id, int currentUserId, string currentUsername, CancellationToken ct = default);
}

public class SalesOrderService(
    ISalesOrderRepository salesOrderRepository,
    IProductRepository productRepository,
    ICustomerRepository customerRepository,
    IInventoryTransactionRepository inventoryTransactionRepository,
    IUnitOfWork unitOfWork,
    IEventPublisher eventPublisher,
    ILogger<SalesOrderService> logger) : ISalesOrderService
{
    public async Task<List<SalesOrderDto>> SearchAsync(DateTime? dateFrom, DateTime? dateTo, int? customerId, CancellationToken ct = default)
    {
        var orders = await salesOrderRepository.SearchAsync(dateFrom, dateTo, customerId, ct);
        return orders.Select(ToDto).ToList();
    }

    public async Task<SalesOrderDto> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var order = await salesOrderRepository.GetByIdAsync(id, ct)
            ?? throw new BusinessRuleException($"找不到出貨單 (Id={id})。");
        return ToDto(order);
    }

    public async Task<SalesOrderDto> CreateAsync(CreateSalesOrderRequest request, int currentUserId, string currentUsername, CancellationToken ct = default)
    {
        if (request.CustomerId is { } customerId && !await customerRepository.ExistsAsync(customerId, ct))
        {
            throw new BusinessRuleException($"找不到客戶 (Id={customerId})。");
        }

        SalesOrder order = new()
        {
            CustomerId = request.CustomerId,
            OrderDate = request.OrderDate ?? DateTime.UtcNow,
            Status = OrderStatus.Normal,
            Note = request.Note.TrimOrNull(),
        };
        order.InitializeAudit(currentUsername);

        var stockAfterByProductId = new Dictionary<int, int>();

        await unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            // 先把這張單需要的商品都讀出來、逐一檢查庫存是否足夠，全部都夠才真的扣庫存，
            // 任何一項不夠就整張單失敗（不允許負庫存，ERP.md §4.4 預設行為）。
            var products = new Dictionary<int, Product>();
            foreach (var itemRequest in request.Items)
            {
                if (!products.ContainsKey(itemRequest.ProductId))
                {
                    var product = await productRepository.GetByIdAsync(itemRequest.ProductId, ct)
                        ?? throw new BusinessRuleException($"找不到商品 (Id={itemRequest.ProductId})。");
                    products[itemRequest.ProductId] = product;
                }
            }

            var requiredQuantityByProductId = request.Items
                .GroupBy(i => i.ProductId)
                .ToDictionary(g => g.Key, g => g.Sum(i => i.Quantity));

            foreach (var (productId, requiredQuantity) in requiredQuantityByProductId)
            {
                var product = products[productId];
                if (product.CurrentStock < requiredQuantity)
                {
                    throw new BusinessRuleException(
                        $"商品「{product.Name}」庫存不足（現有 {product.CurrentStock}，需要 {requiredQuantity}）。");
                }
            }

            order.OrderNo = await GenerateOrderNoAsync("SO", ct);

            foreach (var itemRequest in request.Items)
            {
                var product = products[itemRequest.ProductId];

                order.Items.Add(new SalesOrderItem
                {
                    ProductId = product.Id,
                    Quantity = itemRequest.Quantity,
                    UnitPrice = itemRequest.UnitPrice,
                    Subtotal = itemRequest.UnitPrice * itemRequest.Quantity,
                });

                product.CurrentStock -= itemRequest.Quantity;
                product.TouchUpdated(currentUsername);
                stockAfterByProductId[product.Id] = product.CurrentStock;
            }

            salesOrderRepository.Add(order);
            await unitOfWork.SaveChangesAsync(ct);

            foreach (var item in order.Items)
            {
                inventoryTransactionRepository.Add(new InventoryTransaction
                {
                    ProductId = item.ProductId,
                    ChangeType = InventoryChangeType.Sale,
                    QuantityChange = -item.Quantity,
                    StockAfter = stockAfterByProductId[item.ProductId],
                    RefTable = nameof(SalesOrder),
                    RefId = order.Id,
                    CreatedByUserId = currentUserId,
                    CreatedAt = DateTime.UtcNow,
                });
            }

            await unitOfWork.SaveChangesAsync(ct);
        }, ct);

        var saved = await salesOrderRepository.GetByIdAsync(order.Id, ct)
            ?? throw new InvalidOperationException("出貨單儲存後應該要能查得到，這裡不應該發生。");

        // 出貨單成立會扣庫存，這裡逐一發布「庫存減少」事件，讓 worker 非同步檢查低庫存
        // （2026-09-15 新增，見 Infra-Progress.md §31）。刻意包在 try/catch：Service Bus
        // 萬一暫時連不上，不該讓已經成立的出貨單整筆失敗——訂單/庫存異動才是這支 API 的
        // 主要業務，通知只是錦上添花的 best-effort 附加功能。
        foreach (var productId in stockAfterByProductId.Keys)
        {
            try
            {
                await eventPublisher.PublishInventoryDecreasedAsync(new InventoryDecreasedEvent(productId), ct);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "發布庫存減少事件失敗 (ProductId={ProductId})，不影響出貨單本身已經成立。", productId);
            }
        }

        return ToDto(saved);
    }

    public async Task<SalesOrderDto> VoidAsync(int id, int currentUserId, string currentUsername, CancellationToken ct = default)
    {
        var order = await salesOrderRepository.GetByIdAsync(id, ct)
            ?? throw new BusinessRuleException($"找不到出貨單 (Id={id})。");

        if (order.Status == OrderStatus.Voided)
        {
            throw new BusinessRuleException($"出貨單 {order.OrderNo} 已經作廢過了，不能重複作廢。");
        }

        // 同一個商品可能出現在好幾筆明細，先加總，一次加回庫存。
        var voidQuantityByProductId = order.Items
            .GroupBy(i => i.ProductId)
            .ToDictionary(g => g.Key, g => g.Sum(i => i.Quantity));

        var stockAfterByProductId = new Dictionary<int, int>();
        foreach (var (productId, voidQuantity) in voidQuantityByProductId)
        {
            var product = await productRepository.GetByIdAsync(productId, ct)
                ?? throw new BusinessRuleException($"找不到商品 (Id={productId})。");

            product.CurrentStock += voidQuantity;
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
                ChangeType = InventoryChangeType.SaleVoid,
                QuantityChange = voidQuantity,
                StockAfter = stockAfterByProductId[productId],
                RefTable = nameof(SalesOrder),
                RefId = order.Id,
                CreatedByUserId = currentUserId,
                CreatedAt = DateTime.UtcNow,
            });
        }

        await unitOfWork.SaveChangesAsync(ct);

        return ToDto(order);
    }

    private async Task<string> GenerateOrderNoAsync(string prefix, CancellationToken ct)
    {
        var datePrefix = DateTime.UtcNow.ToString("yyyyMMdd");
        var countToday = await salesOrderRepository.CountByDatePrefixAsync(datePrefix, ct);
        return $"{prefix}-{datePrefix}-{(countToday + 1):D3}";
    }

    private static SalesOrderDto ToDto(SalesOrder order) => new()
    {
        Id = order.Id,
        OrderNo = order.OrderNo,
        CustomerId = order.CustomerId,
        CustomerName = order.Customer?.Name,
        OrderDate = order.OrderDate,
        Status = order.Status.ToString(),
        Note = order.Note,
        TotalAmount = order.Items.Sum(i => i.Subtotal),
        Items = order.Items.Select(i => new SalesOrderItemDto
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
