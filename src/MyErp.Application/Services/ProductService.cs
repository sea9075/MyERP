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

public class ProductService(IProductRepository productRepository, ISupplierRepository supplierRepository, IUnitOfWork unitOfWork)
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
        await ValidateAsync(request, excludeId: null, ct);

        var product = new Product
        {
            // ValidateAsync 已經先把 Sku/Barcode/Name/Unit 修剪過頭尾空白，這裡直接用即可。
            Sku = request.Sku,
            Barcode = request.Barcode,
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

        await ValidateAsync(request, excludeId: id, ct);

        product.Sku = request.Sku;
        product.Barcode = request.Barcode;
        product.Name = request.Name;
        product.CategoryId = request.CategoryId;
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

    private async Task ValidateAsync(CreateProductRequest request, int? excludeId, CancellationToken ct)
    {
        // 先把使用者輸入的字串欄位都修剪過（頭尾空白不算數），後面的查重比對、entity 賦值都直接用
        // 已修剪過的版本，避免 " ABC" 跟 "ABC" 被當成不同的 SKU，也避免資料庫存進帶空白的髒資料。
        request.Sku = request.Sku.TrimRequired();
        request.Name = request.Name.TrimRequired();
        request.Unit = request.Unit.TrimRequired();
        request.Barcode = request.Barcode.TrimOrNull();

        if (await productRepository.SkuExistsAsync(request.Sku, excludeId, ct))
        {
            throw new BusinessRuleException($"商品編號 (SKU) '{request.Sku}' 已經存在。");
        }

        if (request.Barcode != null && await productRepository.BarcodeExistsAsync(request.Barcode, excludeId, ct))
        {
            throw new BusinessRuleException($"條碼 '{request.Barcode}' 已經被其他商品使用。");
        }

        if (request.SupplierId is { } supplierId && !await supplierRepository.ExistsAsync(supplierId, ct))
        {
            throw new BusinessRuleException($"找不到供應商 (Id={supplierId})。");
        }
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
