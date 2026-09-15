using Microsoft.EntityFrameworkCore;
using MyErp.Application.Abstractions;
using MyErp.Domain.Entities;

namespace MyErp.Infra.Repositories;

public class PayrollRecordRepository(MyErpDbContext db) : IPayrollRecordRepository
{
    public async Task<List<PayrollRecord>> SearchAsync(int? employeeId, DateTime? periodMonth, bool includeDeleted, CancellationToken ct = default)
    {
        var query = db.PayrollRecords
            .AsNoTracking()
            .Include(p => p.Employee)
            .ThenInclude(e => e!.User)
            .AsQueryable();

        if (includeDeleted)
        {
            query = query.IgnoreQueryFilters();
        }

        if (employeeId.HasValue)
        {
            query = query.Where(p => p.EmployeeId == employeeId.Value);
        }

        if (periodMonth.HasValue)
        {
            var month = new DateTime(periodMonth.Value.Year, periodMonth.Value.Month, 1);
            query = query.Where(p => p.PeriodMonth == month);
        }

        return await query.OrderByDescending(p => p.PeriodMonth).ToListAsync(ct);
    }

    /// <summary>IgnoreQueryFilters()：用 Id 查詢不受刪除狀態影響。</summary>
    public Task<PayrollRecord?> GetByIdAsync(int id, CancellationToken ct = default) =>
        db.PayrollRecords.IgnoreQueryFilters().Include(p => p.Employee).ThenInclude(e => e!.User)
            .FirstOrDefaultAsync(p => p.Id == id, ct);

    /// <summary>只找「未刪除」的那一筆（Global Query Filter 自動套用）：重新計算前要先把它軟刪除掉。</summary>
    public Task<PayrollRecord?> GetByEmployeeAndMonthAsync(int employeeId, DateTime periodMonth, CancellationToken ct = default)
    {
        var month = new DateTime(periodMonth.Year, periodMonth.Month, 1);
        return db.PayrollRecords.FirstOrDefaultAsync(p => p.EmployeeId == employeeId && p.PeriodMonth == month, ct);
    }

    public void Add(PayrollRecord record) => db.PayrollRecords.Add(record);
}
