using Microsoft.EntityFrameworkCore;
using MyErp.Application.Abstractions;
using MyErp.Domain.Entities;

namespace MyErp.Infra.Repositories;

public class NotificationRepository(MyErpDbContext db) : INotificationRepository
{
    public void Add(Notification notification) => db.Notifications.Add(notification);

    public Task<bool> HasUnreadLowStockAsync(int productId, CancellationToken ct = default) =>
        db.Notifications.AsNoTracking()
            .AnyAsync(n => n.ProductId == productId && n.Type == "LowStock" && !n.IsRead, ct);

    public Task<List<Notification>> GetAllAsync(bool unreadOnly, CancellationToken ct = default)
    {
        var query = db.Notifications.AsNoTracking().Include(n => n.Product).AsQueryable();

        if (unreadOnly)
        {
            query = query.Where(n => !n.IsRead);
        }

        return query.OrderByDescending(n => n.CreatedAt).ThenByDescending(n => n.Id).ToListAsync(ct);
    }

    public Task<int> CountUnreadAsync(CancellationToken ct = default) =>
        db.Notifications.AsNoTracking().CountAsync(n => !n.IsRead, ct);

    /// <summary>不用 AsNoTracking()：NotificationService.MarkReadAsync 要直接改 IsRead 再 SaveChanges。</summary>
    public Task<Notification?> GetByIdAsync(int id, CancellationToken ct = default) =>
        db.Notifications.FirstOrDefaultAsync(n => n.Id == id, ct);

    public Task MarkAllReadAsync(CancellationToken ct = default) =>
        db.Notifications.Where(n => !n.IsRead).ExecuteUpdateAsync(setters => setters.SetProperty(n => n.IsRead, true), ct);
}
