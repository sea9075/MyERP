using Microsoft.EntityFrameworkCore;
using MyErp.Application.Abstractions;
using MyErp.Domain.Entities;

namespace MyErp.Infra.Repositories;

public class ActivityLogRepository(MyErpDbContext db) : IActivityLogRepository
{
    public void Add(ActivityLog log) => db.ActivityLogs.Add(log);

    public Task<List<ActivityLog>> SearchAsync(string? username, DateTime? dateFrom, DateTime? dateTo, CancellationToken ct = default)
    {
        var query = db.ActivityLogs.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(username))
        {
            query = query.Where(l => l.CreatedBy == username);
        }

        if (dateFrom.HasValue)
        {
            query = query.Where(l => l.CreatedAt >= dateFrom.Value);
        }

        if (dateTo.HasValue)
        {
            query = query.Where(l => l.CreatedAt <= dateTo.Value);
        }

        return query.OrderByDescending(l => l.CreatedAt).ThenByDescending(l => l.Id).ToListAsync(ct);
    }
}
