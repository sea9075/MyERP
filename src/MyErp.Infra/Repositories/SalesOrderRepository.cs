using Microsoft.EntityFrameworkCore;
using MyErp.Application.Abstractions;
using MyErp.Domain.Entities;

namespace MyErp.Infra.Repositories;

public class SalesOrderRepository(MyErpDbContext db) : ISalesOrderRepository
{
    public async Task<List<SalesOrder>> SearchAsync(DateTime? dateFrom, DateTime? dateTo, int? customerId, CancellationToken ct = default)
    {
        var query = db.SalesOrders
            .AsNoTracking()
            .Include(o => o.Customer)
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

        if (customerId.HasValue)
        {
            query = query.Where(o => o.CustomerId == customerId.Value);
        }

        return await query.OrderByDescending(o => o.OrderDate).ThenByDescending(o => o.Id).ToListAsync(ct);
    }

    public Task<SalesOrder?> GetByIdAsync(int id, CancellationToken ct = default) =>
        db.SalesOrders
            .Include(o => o.Customer)
            .Include(o => o.Items).ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(o => o.Id == id, ct);

    public Task<int> CountByDatePrefixAsync(string datePrefix, CancellationToken ct = default) =>
        db.SalesOrders.CountAsync(o => o.OrderNo.StartsWith($"SO-{datePrefix}-"), ct);

    public void Add(SalesOrder order) => db.SalesOrders.Add(order);
}
