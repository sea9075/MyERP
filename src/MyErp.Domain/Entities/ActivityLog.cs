namespace MyErp.Domain.Entities;

/// <summary>
/// 操作紀錄（活動日誌）：記錄「誰在什麼時候呼叫了哪一支會改變資料的 API」，方便事後追蹤操作流程。
/// 只記錄 POST/PUT/DELETE/PATCH 這類會改變資料的請求（見 MyErp.Api.Filters.ActivityLogActionFilter），
/// 不記錄單純查詢(GET)。
///
/// 這張表本身就是不可變的日誌紀錄，性質跟 InventoryTransaction 一樣，不套用
/// createdAt/updatedAt/createdBy/updatedBy/isDeleted 那組稽核欄位（沒有「誰建立了這筆日誌」這種
/// 需要再稽核的東西，日誌自己就是稽核紀錄本身）。
/// </summary>
public class ActivityLog
{
    public int Id { get; set; }

    /// <summary>HTTP 方法 + 路徑，例如 "DELETE /api/categories/5"。</summary>
    public string Api { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>操作者的 username；理論上一定是登入使用者（未登入呼叫不了 [Authorize] 的 API）。</summary>
    public string CreatedBy { get; set; } = string.Empty;
}
