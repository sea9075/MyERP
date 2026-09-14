using MyErp.Application.Abstractions;
using MyErp.Application.DTOs;
using MyErp.Domain.Entities;

namespace MyErp.Application.Services;

/// <summary>
/// 「誰在什麼時候做了什麼」的操作紀錄。寫入這張表的呼叫來自 MyErp.Api.Filters.ActivityLogActionFilter
/// （全域 Action Filter，攔截所有 POST/PUT/DELETE/PATCH 請求），不需要在每支 API 手動加程式碼。
/// </summary>
public interface IActivityLogService
{
    /// <summary>
    /// 寫入一筆操作紀錄，並且立刻 SaveChanges（這裡刻意獨立寫入、不等主要業務邏輯的 SaveChanges，
    /// 因為 Action Filter 是在 Controller 動作執行完之後才呼叫，屬於「補記一筆稽核紀錄」的性質）。
    /// </summary>
    Task LogAsync(string api, string username, CancellationToken ct = default);

    Task<List<ActivityLogDto>> SearchAsync(string? username, DateTime? dateFrom, DateTime? dateTo, CancellationToken ct = default);
}

public class ActivityLogService(IActivityLogRepository activityLogRepository, IUnitOfWork unitOfWork) : IActivityLogService
{
    public async Task LogAsync(string api, string username, CancellationToken ct = default)
    {
        activityLogRepository.Add(new ActivityLog
        {
            Api = api,
            CreatedBy = username,
            CreatedAt = DateTime.UtcNow,
        });

        await unitOfWork.SaveChangesAsync(ct);
    }

    public async Task<List<ActivityLogDto>> SearchAsync(string? username, DateTime? dateFrom, DateTime? dateTo, CancellationToken ct = default)
    {
        var logs = await activityLogRepository.SearchAsync(username, dateFrom, dateTo, ct);
        return logs.Select(l => new ActivityLogDto
        {
            Id = l.Id,
            Api = l.Api,
            CreatedAt = l.CreatedAt,
            CreatedBy = l.CreatedBy,
        }).ToList();
    }
}
