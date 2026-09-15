using Microsoft.EntityFrameworkCore;
using MyErp.Application.Abstractions;
using MyErp.Domain.Entities;

namespace MyErp.Infra.Repositories;

public class EmployeeRepository(MyErpDbContext db) : IEmployeeRepository
{
    public async Task<List<Employee>> GetAllAsync(bool includeDeleted, CancellationToken ct = default)
    {
        var query = db.Employees.AsNoTracking().Include(e => e.User).AsQueryable();

        if (includeDeleted)
        {
            query = query.IgnoreQueryFilters();
        }

        return await query.OrderBy(e => e.User!.DisplayName).ToListAsync(ct);
    }

    /// <summary>IgnoreQueryFilters()：用 Id 查詢不受刪除狀態影響（例如修改已離職員工的資料時仍要找得到）。</summary>
    public Task<Employee?> GetByIdAsync(int id, CancellationToken ct = default) =>
        db.Employees.IgnoreQueryFilters().Include(e => e.User)
            .FirstOrDefaultAsync(e => e.Id == id, ct);

    /// <summary>依登入帳號的 UserId 找員工資料，只找「未刪除」的員工（已離職的員工不該還能打卡）。</summary>
    public Task<Employee?> GetByUserIdAsync(int userId, CancellationToken ct = default) =>
        db.Employees.Include(e => e.User).FirstOrDefaultAsync(e => e.UserId == userId, ct);

    public void Add(Employee employee) => db.Employees.Add(employee);
}
