using Microsoft.EntityFrameworkCore;
using MyErp.Application.Abstractions;
using MyErp.Domain.Entities;

namespace MyErp.Infra.Repositories;

public class AttendanceRecordRepository(MyErpDbContext db) : IAttendanceRecordRepository
{
    public async Task<List<AttendanceRecord>> SearchAsync(int? employeeId, DateTime? dateFrom, DateTime? dateTo, bool includeDeleted, CancellationToken ct = default)
    {
        var query = db.AttendanceRecords
            .AsNoTracking()
            .Include(a => a.Employee)
            .ThenInclude(e => e!.User)
            .AsQueryable();

        if (includeDeleted)
        {
            query = query.IgnoreQueryFilters();
        }

        if (employeeId.HasValue)
        {
            query = query.Where(a => a.EmployeeId == employeeId.Value);
        }

        if (dateFrom.HasValue)
        {
            query = query.Where(a => a.WorkDate >= dateFrom.Value.Date);
        }

        if (dateTo.HasValue)
        {
            query = query.Where(a => a.WorkDate <= dateTo.Value.Date);
        }

        return await query.OrderByDescending(a => a.WorkDate).ToListAsync(ct);
    }

    /// <summary>IgnoreQueryFilters()：用 Id 查詢不受刪除狀態影響。</summary>
    public Task<AttendanceRecord?> GetByIdAsync(int id, CancellationToken ct = default) =>
        db.AttendanceRecords.IgnoreQueryFilters().Include(a => a.Employee).ThenInclude(e => e!.User)
            .FirstOrDefaultAsync(a => a.Id == id, ct);

    /// <summary>只找「未刪除」的那一筆（Global Query Filter 自動套用）：刪除後可以讓同一天重新補登。</summary>
    public Task<AttendanceRecord?> GetByEmployeeAndDateAsync(int employeeId, DateTime workDate, CancellationToken ct = default) =>
        db.AttendanceRecords.FirstOrDefaultAsync(a => a.EmployeeId == employeeId && a.WorkDate == workDate.Date, ct);

    public void Add(AttendanceRecord record) => db.AttendanceRecords.Add(record);
}
