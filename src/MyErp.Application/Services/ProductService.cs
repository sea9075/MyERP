using MyErp.Application.Abstractions;
using MyErp.Application.Common;
using MyErp.Application.DTOs;
using MyErp.Domain.Entities;

namespace MyErp.Application.Services;

public interface IProductService
{
    Task<List<ProductDto>> SearchAsync(string? keyword, int? categoryId, bool? lowStock, bool includeDeleted, CancellationToken ct = default);
    Task<ProductDto> GetByIdAsync(int id, CancellationToken ct = default);
    Task<ProductDto> GetByBarcodeAsync(string barcode, CancellationToken ct = default);
    Task<ProductDto> CreateAsync(CreateProductRequest request, string currentUsername, CancellationToken ct = default);
    Task<ProductDto> UpdateAsync(int id, UpdateProductRequest request, string currentUsername, CancellationToken ct = default);

    /// <summary>ERP.md §6：DELETE /api/products/{id} 是軟刪除（IsDeleted=true）。</summary>
    Task DeleteAsync(int id, string currentUsername, CancellationToken ct = default);
}

public class ProductService(
    IProductRepository productRepository,
    ISupplierRepository supplierRepository,
    ICategoryRepository categoryRepository,
    IUnitOfWork unitOfWork)
    : IProductService
{
    public async Task<List<ProductDto>> SearchAsync(string? keyword, int? categoryId, bool? lowStock, bool includeDeleted, CancellationToken ct = default)
    {
        var products = await productRepository.SearchAsync(keyword, categoryId, lowStock, includeDeleted, ct);
        return products.Select(ToDto).ToList();
    }

    public async Task<ProductDto> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var product = await productRepository.GetByIdAsync(id, ct)
            ?? throw new BusinessRuleException($"找不到商品 (Id={id})。");
        return ToDto(product);
    }

    public async Task<ProductDto> GetByBarcodeAsync(string barcode, CancellationToken ct = default)
    {
        var product = await productRepository.GetByBarcodeAsync(barcode, ct)
            ?? throw new BusinessRuleException($"找不到條碼為 {barcode} 的商品。");
        return ToDto(product);
    }

    public async Task<ProductDto> CreateAsync(CreateProductRequest request, string currentUsername, CancellationToken ct = default)
    {
        request.Name = request.Name.TrimRequired();
        request.Unit = request.Unit.TrimRequired();

        var category = await categoryRepository.GetByIdAsync(request.CategoryId, ct)
            ?? throw new BusinessRuleException($"找不到分類 (Id={request.CategoryId})。");
        if (category.IsDeleted)
        {
            throw new BusinessRuleException($"分類「{category.Name}」已經被刪除，無法用來建立商品。");
        }

        if (request.SupplierId is { } supplierId && !await supplierRepository.ExistsAsync(supplierId, ct))
        {
            throw new BusinessRuleException($"找不到供應商 (Id={supplierId})。");
        }

        // 商品標號＝分類編號 + "-" + 7 碼流水號（例如 COK-0000001），條碼＝標號去掉 "-"，
        // 兩者都由系統自動產生，不開放使用者輸入或修改。流水號存在 Category.NextSequence，
        // 跟 PurchaseOrderService.GenerateOrderNoAsync 的單號產生方式一樣：1~5 人低併發情境下
        // 用「讀出來、+1、存檔」已經足夠，嚴格防呆可以之後再改用資料庫序號機制。
        var sequence = category.NextSequence;
        var sku = $"{category.Code}-{sequence:D7}";
        var barcode = sku.Replace("-", string.Empty);

        category.NextSequence = sequence + 1;
        category.TouchUpdated(currentUsername);

        var product = new Product
        {
            Sku = sku,
            Barcode = barcode,
            Name = request.Name,
            CategoryId = request.CategoryId,
            Unit = request.Unit,
            CostPrice = request.CostPrice,
            SalePrice = request.SalePrice,
            SafetyStock = request.SafetyStock,
            SupplierId = request.SupplierId,
            // CurrentStock 刻意不開放在這裡直接設定：新商品一律從 0 開始，
            // 之後只能透過進貨單／出貨單／盤點調整來改變庫存，確保 InventoryTransaction 稽核軌跡完整。
            CurrentStock = 0,
        };
        product.InitializeAudit(currentUsername);

        productRepository.Add(product);
        await unitOfWork.SaveChangesAsync(ct);
        return ToDto(product);
    }

    public async Task<ProductDto> UpdateAsync(int id, UpdateProductRequest request, string currentUsername, CancellationToken ct = default)
    {
        var product = await productRepository.GetByIdAsync(id, ct)
            ?? throw new BusinessRuleException($"找不到商品 (Id={id})。");

        request.Name = request.Name.TrimRequired();
        request.Unit = request.Unit.TrimRequired();

        if (request.SupplierId is { } supplierId && !await supplierRepository.ExistsAsync(supplierId, ct))
        {
            throw new BusinessRuleException($"找不到供應商 (Id={supplierId})。");
        }

        // 分類／商品標號／條碼建立後就固定，這裡刻意不更新（見 UpdateProductRequest 的說明）。
        product.Name = request.Name;
        product.Unit = request.Unit;
        product.CostPrice = request.CostPrice;
        product.SalePrice = request.SalePrice;
        product.SafetyStock = request.SafetyStock;
        product.SupplierId = request.SupplierId;
        product.IsDeleted = request.IsDeleted;
        product.TouchUpdated(currentUsername);

        await unitOfWork.SaveChangesAsync(ct);
        return ToDto(product);
    }

    public async Task DeleteAsync(int id, string currentUsername, CancellationToken ct = default)
    {
        var product = await productRepository.GetByIdAsync(id, ct)
            ?? throw new BusinessRuleException($"找不到商品 (Id={id})。");

        product.SoftDelete(currentUsername);
        await unitOfWork.SaveChangesAsync(ct);
    }

    private static ProductDto ToDto(Product product) => new()
    {
        Id = product.Id,
        Sku = product.Sku,
        Barcode = product.Barcode,
        Name = product.Name,
        CategoryId = product.CategoryId,
        CategoryName = product.Category?.Name,
        Unit = product.Unit,
        CostPrice = product.CostPrice,
        SalePrice = product.SalePrice,
        SafetyStock = product.SafetyStock,
        CurrentStock = product.CurrentStock,
        SupplierId = product.SupplierId,
        SupplierName = product.Supplier?.Name,
        CreatedAt = product.CreatedAt,
        UpdatedAt = product.UpdatedAt,
        CreatedBy = product.CreatedBy,
        UpdatedBy = product.UpdatedBy,
        IsDeleted = product.IsDeleted,
    };
}
