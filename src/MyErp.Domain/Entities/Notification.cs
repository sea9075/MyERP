namespace MyErp.Domain.Entities;

/// <summary>
/// 系統通知（2026-09-15 新增，見 Infra-Progress.md §31）。目前唯一的用途是「低庫存自動通知」：
/// worker 程式訂閱 Azure Service Bus 的庫存異動事件，發現某商品庫存低於安全庫存時，寫入一筆通知。
///
/// 這張表刻意設計成很單純的通知列表，不套用 createdAt/updatedAt/createdBy/updatedBy 那組稽核欄位
/// ——它不是使用者操作產生的業務資料（是 worker 系統產生的），沒有「誰建立/修改了這筆通知」需要
/// 稽核，跟 ActivityLog／InventoryTransaction 是同一種「系統紀錄」性質。但跟那兩張表不同的是，
/// 這張表需要一個可變欄位 IsRead（使用者會把通知標記已讀），所以不是完全不可變的日誌。
/// </summary>
public class Notification
{
    public int Id { get; set; }

    /// <summary>通知類型，目前只有 "LowStock" 一種，用字串保留未來擴充其他類型的空間。</summary>
    public string Type { get; set; } = "LowStock";

    /// <summary>觸發這筆通知的商品。目前唯一的通知類型（低庫存）一定會關聯到商品。</summary>
    public int ProductId { get; set; }
    public Product? Product { get; set; }

    /// <summary>組好的訊息文字，直接顯示給使用者看（例如「商品「可樂」庫存低於安全庫存...」）。</summary>
    public string Message { get; set; } = string.Empty;

    public bool IsRead { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
