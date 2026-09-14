using Microsoft.EntityFrameworkCore;
using MyErp.Application.Abstractions;
using MyErp.Domain.Entities;

namespace MyErp.Infra.Repositories;

public class CustomerRepository(MyErpDbContext db) : ICustomerRepository
{
    /// <summary>只確認「未刪除」的客戶存在（Global Query Filter 自動套用）。</summary>
    public Task<bool> ExistsAsync(int id, CancellationToken ct = default) =>
        db.Customers.AnyAsync(c => c.Id == id, ct);

    public Task<List<Customer>> GetAllAsync(bool includeDeleted, CancellationToken ct = default)
    {
        var query = db.Customers.AsNoTracking().AsQueryable();
        if (includeDeleted)
        {
            query = query.IgnoreQueryFilters();
        }
        return query.OrderBy(c => c.Name).ToListAsync(ct);
    }

    /// <summary>IgnoreQueryFilters()：用 Id 查詢不受刪除狀態影響。</summary>
    public Task<Customer?> GetByIdAsync(int id, CancellationToken ct = default) =>
        db.Customers.IgnoreQueryFilters().FirstOrDefaultAsync(c => c.Id == id, ct);

    /// <summary>SalesOrder 沒有套用軟刪除，這裡查的是「所有」出貨單（含已作廢的）。</summary>
    public Task<bool> HasSalesOrdersAsync(int customerId, CancellationToken ct = default) =>
        db.SalesOrders.AnyAsync(o => o.CustomerId == customerId, ct);

    public void Add(Customer customer) => db.Customers.Add(customer);
}
