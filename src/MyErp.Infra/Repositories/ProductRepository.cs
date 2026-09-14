using Microsoft.EntityFrameworkCore;
using MyErp.Application.Abstractions;
using MyErp.Domain.Entities;

namespace MyErp.Infra.Repositories;

public class ProductRepository(MyErpDbContext db) : IProductRepository
{
    public async Task<List<Product>> SearchAsync(string? keyword, int? categoryId, bool? lowStock, bool includeDeleted, CancellationToken ct = default)
    {
        var query = db.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.Supplier)
            .AsQueryable();

        if (includeDeleted)
        {
            query = query.IgnoreQueryFilters();
        }

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            query = query.Where(p =>
                p.Name.Contains(keyword) ||
                p.Sku.Contains(keyword) ||
                (p.Barcode != null && p.Barcode.Contains(keyword)));
        }

        if (categoryId.HasValue)
        {
            query = query.Where(p => p.CategoryId == categoryId.Value);
        }

        if (lowStock == true)
        {
            query = query.Where(p => p.CurrentStock < p.SafetyStock);
        }

        return await query.OrderBy(p => p.Name).ToListAsync(ct);
    }

    /// <summary>IgnoreQueryFilters()：用 Id 查詢不受刪除狀態影響（例如作廢舊單據時仍要找得到已刪除的商品）。</summary>
    public Task<Product?> GetByIdAsync(int id, CancellationToken ct = default) =>
        db.Products.IgnoreQueryFilters().Include(p => p.Category).Include(p => p.Supplier)
            .FirstOrDefaultAsync(p => p.Id == id, ct);

    /// <summary>條碼掃描查詢：只找「未刪除」的商品（Global Query Filter 自動套用），新交易不該掃到已刪除的商品。</summary>
    public Task<Product?> GetByBarcodeAsync(string barcode, CancellationToken ct = default) =>
        db.Products.Include(p => p.Category).Include(p => p.Supplier)
            .FirstOrDefaultAsync(p => p.Barcode == barcode, ct);

    public Task<bool> SkuExistsAsync(string sku, int? excludeId = null, CancellationToken ct = default) =>
        db.Products.AnyAsync(p => p.Sku == sku && (excludeId == null || p.Id != excludeId), ct);

    public Task<bool> BarcodeExistsAsync(string barcode, int? excludeId = null, CancellationToken ct = default) =>
        db.Products.AnyAsync(p => p.Barcode == barcode && (excludeId == null || p.Id != excludeId), ct);

    public void Add(Product product) => db.Products.Add(product);
}
