using Microsoft.EntityFrameworkCore;
using MyErp.Application.Abstractions;
using MyErp.Domain.Entities;

namespace MyErp.Infra.Repositories;

public class SupplierRepository(MyErpDbContext db) : ISupplierRepository
{
    public Task<List<Supplier>> GetAllAsync(bool includeInactive, CancellationToken ct = default)
    {
        var query = db.Suppliers.AsNoTracking().AsQueryable();
        if (!includeInactive)
        {
            query = query.Where(s => s.IsActive);
        }
        return query.OrderBy(s => s.Name).ToListAsync(ct);
    }

    public Task<Supplier?> GetByIdAsync(int id, CancellationToken ct = default) =>
        db.Suppliers.FirstOrDefaultAsync(s => s.Id == id, ct);

    public Task<bool> ExistsAsync(int id, CancellationToken ct = default) =>
        db.Suppliers.AnyAsync(s => s.Id == id, ct);

    public void Add(Supplier supplier) => db.Suppliers.Add(supplier);
}
