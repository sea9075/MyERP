using MyErp.Domain.Common;

namespace MyErp.Domain.Entities;

/// <summary>
/// 某位員工、某個月份的薪資計算結果（新增：薪資系統）。
///
/// 每次呼叫「計算薪資」（PayrollService.CalculateAsync）都會依當時該月份的出勤紀錄重新計算一次
/// Regular/加班時數與加班費，寫成一筆快照（BaseSalary 也是快照——Employee.MonthlySalary 之後可能會調薪，
/// 已經算過的薪資紀錄不會跟著變動）。BonusAmount 由 HR/Manager/Admin 事後人工輸入，沒有固定公式。
///
/// 加班費計算依勞基法第 24 條的精神：月薪制員工時薪＝月薪 ÷ 30 ÷ 8；平日加班前 2 小時 1.34 倍，
/// 超過 2 小時的部分 1.67 倍。這是目前系統唯一支援的規則，例假日/國定假日出勤的特殊倍率、
/// 請假扣薪等，目前還沒有做（見交付說明），之後有需要再擴充。
///
/// 一個員工、一個月份只會有一筆有效（未刪除）的紀錄，見 MyErpDbContext 的篩選式唯一索引；
/// 重新計算會先軟刪除舊的那一筆，再新增一筆新的，保留歷次計算的軌跡。
/// </summary>
public class PayrollRecord : IAuditable
{
    public int Id { get; set; }

    public int EmployeeId { get; set; }
    public Employee? Employee { get; set; }

    /// <summary>薪資月份的第一天（例如 2026-09-01 代表 2026 年 9 月），時間固定為 00:00:00。</summary>
    public DateTime PeriodMonth { get; set; }

    /// <summary>計算當下的月薪快照。</summary>
    public decimal BaseSalary { get; set; }

    public decimal RegularHours { get; set; }

    /// <summary>平日加班前 2 小時（1.34 倍）的時數加總。</summary>
    public decimal OvertimeHoursTier1 { get; set; }

    /// <summary>平日加班超過 2 小時之後（1.67 倍）的時數加總。</summary>
    public decimal OvertimeHoursTier2 { get; set; }

    /// <summary>依上述規則試算出來的加班費（Tier1 時數×時薪×1.34 + Tier2 時數×時薪×1.67）。</summary>
    public decimal OvertimePay { get; set; }

    /// <summary>獎金，HR/Manager/Admin 人工輸入，預設 0。</summary>
    public decimal BonusAmount { get; set; }

    /// <summary>= BaseSalary + OvertimePay + BonusAmount。</summary>
    public decimal TotalPay { get; set; }

    public string? Note { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public string CreatedBy { get; set; } = string.Empty;
    public string UpdatedBy { get; set; } = string.Empty;
    public bool IsDeleted { get; set; }
}
