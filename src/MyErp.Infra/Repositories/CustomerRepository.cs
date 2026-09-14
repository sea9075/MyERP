using Microsoft.EntityFrameworkCore;
using MyErp.Application.Abstractions;

namespace MyErp.Infra.Repositories;

/// <summary>
/// Phase 1 只需要驗證 CustomerId 存不存在（SalesOrder 的 FK 檢查用）。
/// 完整的客戶 CRUD 依 ERP.md §8 排到 Phase 2，所以這裡先不做 GetAll/Add 之類的方法。
/// </summary>
public class CustomerRepository(MyErpDbContext db) : ICustomerRepository
{
    public Task<bool> ExistsAsync(int id, CancellationToken ct = default) =>
        db.Customers.AnyAsync(c => c.Id == id, ct);
}
