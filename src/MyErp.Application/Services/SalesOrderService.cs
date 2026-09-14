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
    /// </summary>
    Task<SalesOrderDto> CreateAsync(CreateSalesOrderRequest request, int currentUserId, CancellationToken ct = default);
}

public class SalesOrderService(
    ISalesOrderRepository salesOrderRepository,
    IProductRepository productRepository,
    ICustomerRepository customerRepository,
    IInventoryTransactionRepository inventoryTransactionRepository,
    IUnitOfWork unitOfWork) : ISalesOrderService
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

    public async Task<SalesOrderDto> CreateAsync(CreateSalesOrderRequest request, int currentUserId, CancellationToken ct = default)
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
            Note = request.Note,
            CreatedByUserId = currentUserId,
            CreatedAt = DateTime.UtcNow,
        };

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
                product.UpdatedAt = DateTime.UtcNow;
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
        return ToDto(saved);
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
    };
}
