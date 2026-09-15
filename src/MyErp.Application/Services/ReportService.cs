using MyErp.Application.Abstractions;
using MyErp.Application.Common;
using MyErp.Application.DTOs;
using MyErp.Domain.Entities;
using MyErp.Domain.Enums;

namespace MyErp.Application.Services;

/// <summary>
/// 報表模組（ERP.md §4.6，Infra-Progress.md §27）：進貨統計、銷售統計、毛利報表、庫存總覽/低庫存清單。
/// 只有 Manager/Admin 能用，見 ReportsController 的 [Authorize]。
/// </summary>
public interface IReportService
{
    Task<PurchaseReportDto> GetPurchaseReportAsync(
        DateTime dateFrom, DateTime dateTo, int? supplierId, int? productId, int? categoryId, CancellationToken ct = default);

    Task<SalesReportDto> GetSalesReportAsync(
        DateTime dateFrom, DateTime dateTo, int? customerId, int? productId, int? categoryId, CancellationToken ct = default);

    Task<GrossMarginReportDto> GetGrossMarginReportAsync(
        DateTime dateFrom, DateTime dateTo, int? productId, int? categoryId, string groupBy, CancellationToken ct = default);

    Task<InventoryReportDto> GetInventoryReportAsync(int? categoryId, bool lowStockOnly, CancellationToken ct = default);
}

public class ReportService(
    IPurchaseOrderRepository purchaseOrderRepository,
    ISalesOrderRepository salesOrderRepository,
    IProductRepository productRepository,
    ICategoryRepository categoryRepository) : IReportService
{
    public async Task<PurchaseReportDto> GetPurchaseReportAsync(
        DateTime dateFrom, DateTime dateTo, int? supplierId, int? productId, int? categoryId, CancellationToken ct = default)
    {
        var categoryNames = await GetCategoryNameLookupAsync(ct);

        var orders = (await purchaseOrderRepository.SearchAsync(dateFrom, dateTo, supplierId, ct))
            .Where(o => o.Status != OrderStatus.Voided)
            .ToList();

        // 有帶商品/分類篩選時，只留下「這張單裡至少有一筆符合篩選的明細」的單，
        // 而且彙總金額也只算符合篩選的明細，不是整張單的金額（見 DTO 註解）。
        var matched = orders
            .Select(o => new
            {
                Order = o,
                Items = o.Items.Where(i =>
                    (!productId.HasValue || i.ProductId == productId.Value) &&
                    (!categoryId.HasValue || (i.Product != null && i.Product.CategoryId == categoryId.Value))).ToList(),
            })
            .Where(x => x.Items.Count > 0)
            .ToList();

        var orderRows = matched.Select(x => new PurchaseReportOrderRowDto
        {
            OrderId = x.Order.Id,
            OrderNo = x.Order.OrderNo,
            OrderDate = x.Order.OrderDate,
            SupplierId = x.Order.SupplierId,
            SupplierName = x.Order.Supplier?.Name ?? string.Empty,
            TotalAmount = x.Items.Sum(i => i.Subtotal),
        }).ToList();

        var bySupplier = matched
            .GroupBy(x => new { x.Order.SupplierId, SupplierName = x.Order.Supplier?.Name ?? string.Empty })
            .Select(g => new PurchaseReportBySupplierDto
            {
                SupplierId = g.Key.SupplierId,
                SupplierName = g.Key.SupplierName,
                OrderCount = g.Count(),
                TotalQuantity = g.SelectMany(x => x.Items).Sum(i => i.Quantity),
                TotalAmount = g.SelectMany(x => x.Items).Sum(i => i.Subtotal),
            })
            .OrderByDescending(r => r.TotalAmount)
            .ToList();

        var byProduct = matched
            .SelectMany(x => x.Items)
            .GroupBy(i => new { i.ProductId, Sku = i.Product?.Sku ?? string.Empty, ProductName = i.Product?.Name ?? string.Empty })
            .Select(g =>
            {
                var totalQuantity = g.Sum(i => i.Quantity);
                var totalAmount = g.Sum(i => i.Subtotal);
                var categoryIdForProduct = g.First().Product?.CategoryId;
                return new PurchaseReportByProductDto
                {
                    ProductId = g.Key.ProductId,
                    Sku = g.Key.Sku,
                    ProductName = g.Key.ProductName,
                    CategoryName = categoryIdForProduct.HasValue && categoryNames.TryGetValue(categoryIdForProduct.Value, out var name) ? name : null,
                    TotalQuantity = totalQuantity,
                    TotalAmount = totalAmount,
                    AverageUnitPrice = totalQuantity == 0 ? 0 : Math.Round(totalAmount / totalQuantity, 2),
                };
            })
            .OrderByDescending(r => r.TotalAmount)
            .ToList();

        return new PurchaseReportDto
        {
            DateFrom = dateFrom,
            DateTo = dateTo,
            TotalOrderCount = orderRows.Count,
            TotalQuantity = orderRows.Count == 0 ? 0 : byProduct.Sum(r => r.TotalQuantity),
            TotalAmount = orderRows.Sum(r => r.TotalAmount),
            BySupplier = bySupplier,
            ByProduct = byProduct,
            Orders = orderRows.OrderByDescending(r => r.OrderDate).ThenByDescending(r => r.OrderId).ToList(),
        };
    }

    public async Task<SalesReportDto> GetSalesReportAsync(
        DateTime dateFrom, DateTime dateTo, int? customerId, int? productId, int? categoryId, CancellationToken ct = default)
    {
        var categoryNames = await GetCategoryNameLookupAsync(ct);

        var orders = (await salesOrderRepository.SearchAsync(dateFrom, dateTo, customerId, ct))
            .Where(o => o.Status != OrderStatus.Voided)
            .ToList();

        var matched = orders
            .Select(o => new
            {
                Order = o,
                Items = o.Items.Where(i =>
                    (!productId.HasValue || i.ProductId == productId.Value) &&
                    (!categoryId.HasValue || (i.Product != null && i.Product.CategoryId == categoryId.Value))).ToList(),
            })
            .Where(x => x.Items.Count > 0)
            .ToList();

        var orderRows = matched.Select(x => new SalesReportOrderRowDto
        {
            OrderId = x.Order.Id,
            OrderNo = x.Order.OrderNo,
            OrderDate = x.Order.OrderDate,
            CustomerId = x.Order.CustomerId,
            CustomerName = x.Order.CustomerId.HasValue ? (x.Order.Customer?.Name ?? string.Empty) : "一般散客",
            TotalAmount = x.Items.Sum(i => i.Subtotal),
        }).ToList();

        var byCustomer = matched
            .GroupBy(x => new
            {
                x.Order.CustomerId,
                CustomerName = x.Order.CustomerId.HasValue ? (x.Order.Customer?.Name ?? string.Empty) : "一般散客",
            })
            .Select(g => new SalesReportByCustomerDto
            {
                CustomerId = g.Key.CustomerId,
                CustomerName = g.Key.CustomerName,
                OrderCount = g.Count(),
                TotalAmount = g.SelectMany(x => x.Items).Sum(i => i.Subtotal),
            })
            .OrderByDescending(r => r.TotalAmount)
            .ToList();

        var byProduct = matched
            .SelectMany(x => x.Items)
            .GroupBy(i => new { i.ProductId, Sku = i.Product?.Sku ?? string.Empty, ProductName = i.Product?.Name ?? string.Empty })
            .Select(g =>
            {
                var totalQuantity = g.Sum(i => i.Quantity);
                var totalAmount = g.Sum(i => i.Subtotal);
                var categoryIdForProduct = g.First().Product?.CategoryId;
                return new SalesReportByProductDto
                {
                    ProductId = g.Key.ProductId,
                    Sku = g.Key.Sku,
                    ProductName = g.Key.ProductName,
                    CategoryName = categoryIdForProduct.HasValue && categoryNames.TryGetValue(categoryIdForProduct.Value, out var name) ? name : null,
                    TotalQuantity = totalQuantity,
                    TotalAmount = totalAmount,
                    AverageUnitPrice = totalQuantity == 0 ? 0 : Math.Round(totalAmount / totalQuantity, 2),
                };
            })
            .OrderByDescending(r => r.TotalAmount)
            .ToList();

        return new SalesReportDto
        {
            DateFrom = dateFrom,
            DateTo = dateTo,
            TotalOrderCount = orderRows.Count,
            TotalQuantity = byProduct.Sum(r => r.TotalQuantity),
            TotalAmount = orderRows.Sum(r => r.TotalAmount),
            ByCustomer = byCustomer,
            ByProduct = byProduct,
            Orders = orderRows.OrderByDescending(r => r.OrderDate).ThenByDescending(r => r.OrderId).ToList(),
        };
    }

    public async Task<GrossMarginReportDto> GetGrossMarginReportAsync(
        DateTime dateFrom, DateTime dateTo, int? productId, int? categoryId, string groupBy, CancellationToken ct = default)
    {
        if (groupBy != "product" && groupBy != "category")
        {
            throw new BusinessRuleException("groupBy 只能是 \"product\" 或 \"category\"。");
        }

        var categoryNames = await GetCategoryNameLookupAsync(ct);

        var items = (await salesOrderRepository.SearchAsync(dateFrom, dateTo, customerId: null, ct))
            .Where(o => o.Status != OrderStatus.Voided)
            .SelectMany(o => o.Items)
            .Where(i =>
                (!productId.HasValue || i.ProductId == productId.Value) &&
                (!categoryId.HasValue || (i.Product != null && i.Product.CategoryId == categoryId.Value)))
            .ToList();

        List<GrossMarginRowDto> rows;
        if (groupBy == "product")
        {
            rows = items
                .GroupBy(i => new { i.ProductId, Sku = i.Product?.Sku ?? string.Empty, Name = i.Product?.Name ?? string.Empty, CostPrice = i.Product?.CostPrice ?? 0m, i.Product?.CategoryId })
                .Select(g =>
                {
                    var quantitySold = g.Sum(i => i.Quantity);
                    var salesAmount = g.Sum(i => i.Subtotal);
                    var costAmount = quantitySold * g.Key.CostPrice;
                    var grossProfit = salesAmount - costAmount;
                    return new GrossMarginRowDto
                    {
                        ProductId = g.Key.ProductId,
                        Sku = g.Key.Sku,
                        Name = g.Key.Name,
                        CategoryName = g.Key.CategoryId.HasValue && categoryNames.TryGetValue(g.Key.CategoryId.Value, out var name) ? name : null,
                        QuantitySold = quantitySold,
                        SalesAmount = salesAmount,
                        CostAmount = costAmount,
                        GrossProfit = grossProfit,
                        GrossMarginPercent = salesAmount == 0 ? 0 : Math.Round(grossProfit / salesAmount * 100, 2),
                    };
                })
                .OrderByDescending(r => r.GrossProfit)
                .ToList();
        }
        else
        {
            rows = items
                .GroupBy(i => i.Product?.CategoryId)
                .Select(g =>
                {
                    var quantitySold = g.Sum(i => i.Quantity);
                    var salesAmount = g.Sum(i => i.Subtotal);
                    var costAmount = g.Sum(i => i.Quantity * (i.Product?.CostPrice ?? 0m));
                    var grossProfit = salesAmount - costAmount;
                    var categoryName = g.Key.HasValue && categoryNames.TryGetValue(g.Key.Value, out var name) ? name : "（未分類）";
                    return new GrossMarginRowDto
                    {
                        ProductId = null,
                        Sku = null,
                        Name = categoryName,
                        CategoryName = categoryName,
                        QuantitySold = quantitySold,
                        SalesAmount = salesAmount,
                        CostAmount = costAmount,
                        GrossProfit = grossProfit,
                        GrossMarginPercent = salesAmount == 0 ? 0 : Math.Round(grossProfit / salesAmount * 100, 2),
                    };
                })
                .OrderByDescending(r => r.GrossProfit)
                .ToList();
        }

        var totalSalesAmount = rows.Sum(r => r.SalesAmount);
        var totalCostAmount = rows.Sum(r => r.CostAmount);
        var totalGrossProfit = totalSalesAmount - totalCostAmount;

        return new GrossMarginReportDto
        {
            DateFrom = dateFrom,
            DateTo = dateTo,
            GroupBy = groupBy,
            Rows = rows,
            TotalSalesAmount = totalSalesAmount,
            TotalCostAmount = totalCostAmount,
            TotalGrossProfit = totalGrossProfit,
            OverallGrossMarginPercent = totalSalesAmount == 0 ? 0 : Math.Round(totalGrossProfit / totalSalesAmount * 100, 2),
        };
    }

    public async Task<InventoryReportDto> GetInventoryReportAsync(int? categoryId, bool lowStockOnly, CancellationToken ct = default)
    {
        var products = await productRepository.SearchAsync(
            keyword: null, categoryId: categoryId, lowStock: lowStockOnly ? true : null, includeDeleted: false, ct);

        var rows = products.Select(p => new InventoryReportRowDto
        {
            ProductId = p.Id,
            Sku = p.Sku,
            Name = p.Name,
            CategoryName = p.Category?.Name,
            Unit = p.Unit,
            CurrentStock = p.CurrentStock,
            SafetyStock = p.SafetyStock,
            IsLowStock = p.CurrentStock < p.SafetyStock,
            CostPrice = p.CostPrice,
            EstimatedValue = p.CurrentStock * p.CostPrice,
        }).ToList();

        return new InventoryReportDto
        {
            Rows = rows,
            LowStockCount = rows.Count(r => r.IsLowStock),
            TotalEstimatedValue = rows.Sum(r => r.EstimatedValue),
        };
    }

    /// <summary>Id → 分類名稱的查詢表，報表裡「依商品彙總」時要顯示分類名稱用（Product 本身只帶 CategoryId，見 §27）。</summary>
    private async Task<Dictionary<int, string>> GetCategoryNameLookupAsync(CancellationToken ct) =>
        (await categoryRepository.GetAllAsync(includeDeleted: true, ct)).ToDictionary(c => c.Id, c => c.Name);
}
