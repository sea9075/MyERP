using Microsoft.EntityFrameworkCore;
using MyErp.Application.Abstractions;
using MyErp.Domain.Entities;

namespace MyErp.Infra.Repositories;

public class PurchaseOrderRepository(MyErpDbContext db) : IPurchaseOrderRepository
{
    public async Task<List<PurchaseOrder>> SearchAsync(DateTime? dateFrom, DateTime? dateTo, int? supplierId, CancellationToken ct = default)
    {
        var query = db.PurchaseOrders
            .AsNoTracking()
            .Include(o => o.Supplier)
            .Include(o => o.Items).ThenInclude(i => i.Product)
            .AsQueryable();

        if (dateFrom.HasValue)
        {
            query = query.Where(o => o.OrderDate >= dateFrom.Value);
        }

        if (dateTo.HasValue)
        {
            query = query.Where(o => o.OrderDate <= dateTo.Value);
        }

        if (supplierId.HasValue)
        {
            query = query.Where(o => o.SupplierId == supplierId.Value);
        }

        return await query.OrderByDescending(o => o.OrderDate).ThenByDescending(o => o.Id).ToListAsync(ct);
    }

    public Task<PurchaseOrder?> GetByIdAsync(int id, CancellationToken ct = default) =>
        db.PurchaseOrders
            .Include(o => o.Supplier)
            .Include(o => o.Items).ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(o => o.Id == id, ct);

    public Task<int> CountByDatePrefixAsync(string datePrefix, CancellationToken ct = default) =>
        db.PurchaseOrders.CountAsync(o => o.OrderNo.StartsWith($"PI-{datePrefix}-"), ct);

    public void Add(PurchaseOrder order) => db.PurchaseOrders.Add(order);
}
