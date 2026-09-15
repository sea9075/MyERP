using MyErp.Domain.Common;
using MyErp.Domain.Enums;

namespace MyErp.Domain.Entities;

/// <summary>
/// 每日出勤紀錄（新增：薪資/出勤系統）。一位員工、一天最多一筆，記錄上班/下班時間。
///
/// Source 記錄這筆是員工自己打卡（SelfService，透過登入帳號呼叫「上班/下班打卡」API）還是
/// HR/Manager/Admin 事後手動建立或補登（ManualEntry，例如忘記打卡、系統問題、請假但仍要記錄）——
/// 需求本來就是「兩者都要」，所以同一張表同時支援兩種來源，用 Source 欄位區分，方便之後追蹤。
///
/// 套用軟刪除（IAuditable）：HR 打錯資料需要能刪掉重建，又要保留「是誰、什麼時候改的」稽核軌跡，
/// 跟 Category/Product 等主檔的軟刪除邏輯一致。
/// </summary>
public class AttendanceRecord : IAuditable
{
    public int Id { get; set; }

    public int EmployeeId { get; set; }
    public Employee? Employee { get; set; }

    /// <summary>
    /// 這筆出勤紀錄屬於哪一天。刻意只存日期（時間固定為當天 00:00:00），代表「這是哪一天的出勤」，
    /// 不受打卡當下實際時間影響分組；同一位員工、同一天只能有一筆未刪除的紀錄
    /// （見 MyErpDbContext 的篩選式唯一索引）。
    /// </summary>
    public DateTime WorkDate { get; set; }

    public DateTime? ClockInAt { get; set; }
    public DateTime? ClockOutAt { get; set; }

    public AttendanceSource Source { get; set; } = AttendanceSource.SelfService;

    /// <summary>備註，例如「忘記打卡，事後補登」「請假半天」。</summary>
    public string? Note { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public string CreatedBy { get; set; } = string.Empty;
    public string UpdatedBy { get; set; } = string.Empty;
    public bool IsDeleted { get; set; }
}
