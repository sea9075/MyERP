using MyErp.Domain.Common;

namespace MyErp.Application.Common;

/// <summary>
/// 統一設定稽核欄位的小工具，讓每個 Service 不用各自重複寫「建立時 4 個欄位設一樣的值」、
/// 「更新時只改 UpdatedAt/UpdatedBy」這種邏輯。
/// </summary>
public static class AuditableExtensions
{
    /// <summary>
    /// 新增資料時呼叫：CreatedAt/UpdatedAt/CreatedBy/UpdatedBy 這 4 個欄位一開始都設成一樣的值。
    /// </summary>
    public static void InitializeAudit(this ITrackable entity, string currentUsername)
    {
        var now = DateTime.UtcNow;
        entity.CreatedAt = now;
        entity.UpdatedAt = now;
        entity.CreatedBy = currentUsername;
        entity.UpdatedBy = currentUsername;
    }

    /// <summary>
    /// 修改資料時呼叫（一般的更新、或作廢/軟刪除這類「狀態變更」也算一次異動）：
    /// 只更新 UpdatedAt/UpdatedBy，CreatedAt/CreatedBy 維持不變。
    /// </summary>
    public static void TouchUpdated(this ITrackable entity, string currentUsername)
    {
        entity.UpdatedAt = DateTime.UtcNow;
        entity.UpdatedBy = currentUsername;
    }

    /// <summary>
    /// 軟刪除：設定 IsDeleted=true，並比照一般更新，同時記錄是誰、什麼時候做的（UpdatedAt/UpdatedBy）。
    /// 刻意不另外設計 DeletedAt/DeletedBy 欄位——查詢「誰在什麼時候刪除的」，
    /// 直接看 IsDeleted=true 時的 UpdatedAt/UpdatedBy 就好。
    /// </summary>
    public static void SoftDelete(this IAuditable entity, string currentUsername)
    {
        entity.IsDeleted = true;
        entity.TouchUpdated(currentUsername);
    }
}
