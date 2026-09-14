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
            // .Contains() 底層會轉成參數化的 SQL LIKE（EF Core 自動處理，不會有 SQL Injection 風險），
            // 但使用者輸入的關鍵字如果剛好含有 %、_、[ 這幾個 LIKE 萬用字元，仍會被 SQL Server
            // 當成萬用字元解讀（例如搜尋 "50%" 會變成比對任意字元），屬於行為上的意外而非資安漏洞，
            // 這裡先跳脫成字面值再查，讓「搜尋什麼就比對什麼」。
            var escapedKeyword = EscapeLikeWildcards(keyword);
            query = query.Where(p =>
                EF.Functions.Like(p.Name, $"%{escapedKeyword}%") ||
                EF.Functions.Like(p.Sku, $"%{escapedKeyword}%") ||
                (p.Barcode != null && EF.Functions.Like(p.Barcode, $"%{escapedKeyword}%")));
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

    /// <summary>
    /// 把 SQL Server LIKE 語法裡的萬用字元（%、_、[）都跳脫成字面值，這樣使用者搜尋的關鍵字
    /// 不管內容是什麼，都只會被當成「純文字比對」，不會被解讀成萬用字元或字元範圍。
    /// 一定要先跳脫 [ 再跳脫 %、_，不然後面兩步驟插入的中括號會被第一步驟誤判成要跳脫的對象。
    /// </summary>
    private static string EscapeLikeWildcards(string value) =>
        value.Replace("[", "[[]").Replace("%", "[%]").Replace("_", "[_]");
}
