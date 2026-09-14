using Microsoft.EntityFrameworkCore;
using MyErp.Application.Abstractions;
using MyErp.Domain.Entities;

namespace MyErp.Infra.Repositories;

public class CategoryRepository(MyErpDbContext db) : ICategoryRepository
{
    public Task<List<Category>> GetAllAsync(CancellationToken ct = default) =>
        db.Categories.AsNoTracking().OrderBy(c => c.Name).ToListAsync(ct);

    public Task<Category?> GetByIdAsync(int id, CancellationToken ct = default) =>
        db.Categories.FirstOrDefaultAsync(c => c.Id == id, ct);

    public Task<bool> HasProductsAsync(int categoryId, CancellationToken ct = default) =>
        db.Products.AnyAsync(p => p.CategoryId == categoryId, ct);

    public void Add(Category category) => db.Categories.Add(category);

    public void Remove(Category category) => db.Categories.Remove(category);
}
