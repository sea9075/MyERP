using MyErp.Application.Abstractions;
using MyErp.Application.Common;
using MyErp.Application.DTOs;
using MyErp.Domain.Entities;

namespace MyErp.Application.Services;

public interface IProductService
{
    Task<List<ProductDto>> SearchAsync(string? keyword, int? categoryId, bool? lowStock, CancellationToken ct = default);
    Task<ProductDto> GetByIdAsync(int id, CancellationToken ct = default);
    Task<ProductDto> GetByBarcodeAsync(string barcode, CancellationToken ct = default);
    Task<ProductDto> CreateAsync(CreateProductRequest request, CancellationToken ct = default);
    Task<ProductDto> UpdateAsync(int id, UpdateProductRequest request, CancellationToken ct = default);

    /// <summary>ERP.md §6：DELETE /api/products/{id} 是軟刪除（IsActive=false）。</summary>
    Task DeleteAsync(int id, CancellationToken ct = default);
}

public class ProductService(IProductRepository productRepository, ISupplierRepository supplierRepository, IUnitOfWork unitOfWork)
    : IProductService
{
    public async Task<List<ProductDto>> SearchAsync(string? keyword, int? categoryId, bool? lowStock, CancellationToken ct = default)
    {
        var products = await productRepository.SearchAsync(keyword, categoryId, lowStock, ct);
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

    public async Task<ProductDto> CreateAsync(CreateProductRequest request, CancellationToken ct = default)
    {
        await ValidateAsync(request, excludeId: null, ct);

        var product = new Product
        {
            Sku = request.Sku,
            Barcode = string.IsNullOrWhiteSpace(request.Barcode) ? null : request.Barcode,
            Name = request.Name,
            CategoryId = request.CategoryId,
            Unit = request.Unit,
            CostPrice = request.CostPrice,
            SalePrice = request.SalePrice,
            SafetyStock = request.SafetyStock,
            SupplierId = request.SupplierId,
            IsActive = true,
            // CurrentStock 刻意不開放在這裡直接設定：新商品一律從 0 開始，
            // 之後只能透過進貨單／出貨單／（Phase 2）盤點調整來改變庫存，確保 InventoryTransaction 稽核軌跡完整。
            CurrentStock = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        productRepository.Add(product);
        await unitOfWork.SaveChangesAsync(ct);
        return ToDto(product);
    }

    public async Task<ProductDto> UpdateAsync(int id, UpdateProductRequest request, CancellationToken ct = default)
    {
        var product = await productRepository.GetByIdAsync(id, ct)
            ?? throw new BusinessRuleException($"找不到商品 (Id={id})。");

        await ValidateAsync(request, excludeId: id, ct);

        product.Sku = request.Sku;
        product.Barcode = string.IsNullOrWhiteSpace(request.Barcode) ? null : request.Barcode;
        product.Name = request.Name;
        product.CategoryId = request.CategoryId;
        product.Unit = request.Unit;
        product.CostPrice = request.CostPrice;
        product.SalePrice = request.SalePrice;
        product.SafetyStock = request.SafetyStock;
        product.SupplierId = request.SupplierId;
        product.IsActive = request.IsActive;
        product.UpdatedAt = DateTime.UtcNow;

        await unitOfWork.SaveChangesAsync(ct);
        return ToDto(product);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var product = await productRepository.GetByIdAsync(id, ct)
            ?? throw new BusinessRuleException($"找不到商品 (Id={id})。");

        product.IsActive = false;
        product.UpdatedAt = DateTime.UtcNow;
        await unitOfWork.SaveChangesAsync(ct);
    }

    private async Task ValidateAsync(CreateProductRequest request, int? excludeId, CancellationToken ct)
    {
        if (await productRepository.SkuExistsAsync(request.Sku, excludeId, ct))
        {
            throw new BusinessRuleException($"商品編號 (SKU) '{request.Sku}' 已經存在。");
        }

        if (!string.IsNullOrWhiteSpace(request.Barcode) && await productRepository.BarcodeExistsAsync(request.Barcode, excludeId, ct))
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
        IsActive = product.IsActive,
    };
}
