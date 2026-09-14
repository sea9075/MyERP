using Microsoft.EntityFrameworkCore;
using MyErp.Application.Abstractions;
using MyErp.Domain.Entities;

namespace MyErp.Infra.Repositories;

public class CategoryRepository(MyErpDbContext db) : ICategoryRepository
{
    public Task<List<Category>> GetAllAsync(bool includeDeleted, CancellationToken ct = default)
    {
        var query = db.Categories.AsNoTracking().AsQueryable();
        if (includeDeleted)
        {
            query = query.IgnoreQueryFilters();
        }
        return query.OrderBy(c => c.Name).ToListAsync(ct);
    }

    /// <summary>IgnoreQueryFilters()：用 Id 查詢不受刪除狀態影響。</summary>
    public Task<Category?> GetByIdAsync(int id, CancellationToken ct = default) =>
        db.Categories.IgnoreQueryFilters().FirstOrDefaultAsync(c => c.Id == id, ct);

    /// <summary>Product 的 Global Query Filter 自動套用，這裡只會算到「未刪除」的商品。</summary>
    public Task<bool> HasProductsAsync(int categoryId, CancellationToken ct = default) =>
        db.Products.AnyAsync(p => p.CategoryId == categoryId, ct);

    public void Add(Category category) => db.Categories.Add(category);
}
