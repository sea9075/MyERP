using Microsoft.EntityFrameworkCore;
using MyErp.Application.Abstractions;
using MyErp.Domain.Entities;

namespace MyErp.Infra.Repositories;

public class SupplierRepository(MyErpDbContext db) : ISupplierRepository
{
    public Task<List<Supplier>> GetAllAsync(bool includeDeleted, CancellationToken ct = default)
    {
        var query = db.Suppliers.AsNoTracking().AsQueryable();
        if (includeDeleted)
        {
            query = query.IgnoreQueryFilters();
        }
        return query.OrderBy(s => s.Name).ToListAsync(ct);
    }

    /// <summary>IgnoreQueryFilters()：用 Id 查詢不受刪除狀態影響。</summary>
    public Task<Supplier?> GetByIdAsync(int id, CancellationToken ct = default) =>
        db.Suppliers.IgnoreQueryFilters().FirstOrDefaultAsync(s => s.Id == id, ct);

    /// <summary>只確認「未刪除」的供應商存在（Global Query Filter 自動套用）。</summary>
    public Task<bool> ExistsAsync(int id, CancellationToken ct = default) =>
        db.Suppliers.AnyAsync(s => s.Id == id, ct);

    /// <summary>不加 IgnoreQueryFilters()：只跟「未刪除」的供應商比對，已軟刪除的名稱可以被重複使用。</summary>
    public Task<bool> NameExistsAsync(string name, int? excludeId = null, CancellationToken ct = default) =>
        db.Suppliers.AnyAsync(s => s.Name == name && (excludeId == null || s.Id != excludeId), ct);

    public void Add(Supplier supplier) => db.Suppliers.Add(supplier);
}
