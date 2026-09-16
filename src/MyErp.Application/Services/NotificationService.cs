using MyErp.Application.Abstractions;
using MyErp.Application.Common;
using MyErp.Application.DTOs;
using MyErp.Domain.Entities;

namespace MyErp.Application.Services;

/// <summary>
/// 通知列表的 API 端邏輯（列表／未讀數／標記已讀），給 MyErp.Api 的 NotificationsController 用。
/// 「寫入」通知的業務邏輯不在這裡，是 worker 端專用的 IInventoryEventHandler（見該檔案的說明）
/// ——兩者職責刻意分開：這個 service 只讀、只改 IsRead，真正判斷「要不要產生通知」的邏輯
/// 只存在於 worker 那一份，避免同一套規則要維護兩個地方。
/// </summary>
public interface INotificationService
{
    Task<List<NotificationDto>> GetAllAsync(bool unreadOnly, CancellationToken ct = default);

    Task<int> CountUnreadAsync(CancellationToken ct = default);

    Task MarkReadAsync(int id, CancellationToken ct = default);

    Task MarkAllReadAsync(CancellationToken ct = default);
}

public class NotificationService(INotificationRepository notificationRepository, IUnitOfWork unitOfWork) : INotificationService
{
    public async Task<List<NotificationDto>> GetAllAsync(bool unreadOnly, CancellationToken ct = default)
    {
        var notifications = await notificationRepository.GetAllAsync(unreadOnly, ct);
        return notifications.Select(ToDto).ToList();
    }

    public Task<int> CountUnreadAsync(CancellationToken ct = default) =>
        notificationRepository.CountUnreadAsync(ct);

    public async Task MarkReadAsync(int id, CancellationToken ct = default)
    {
        var notification = await notificationRepository.GetByIdAsync(id, ct)
            ?? throw new BusinessRuleException($"找不到通知 (Id={id})。");

        notification.IsRead = true;
        await unitOfWork.SaveChangesAsync(ct);
    }

    public Task MarkAllReadAsync(CancellationToken ct = default) =>
        notificationRepository.MarkAllReadAsync(ct);

    private static NotificationDto ToDto(Notification notification) => new()
    {
        Id = notification.Id,
        Type = notification.Type,
        ProductId = notification.ProductId,
        ProductName = notification.Product?.Name,
        Message = notification.Message,
        IsRead = notification.IsRead,
        CreatedAt = notification.CreatedAt,
    };
}
