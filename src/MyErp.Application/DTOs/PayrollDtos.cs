using System.ComponentModel.DataAnnotations;

namespace MyErp.Application.DTOs;

public class PayrollRecordDto
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public string? EmployeeName { get; set; }

    /// <summary>薪資月份，例如 2026-09-01 代表 2026 年 9 月。</summary>
    public DateTime PeriodMonth { get; set; }

    public decimal BaseSalary { get; set; }
    public decimal RegularHours { get; set; }
    public decimal OvertimeHoursTier1 { get; set; }
    public decimal OvertimeHoursTier2 { get; set; }
    public decimal OvertimePay { get; set; }
    public decimal BonusAmount { get; set; }
    public decimal TotalPay { get; set; }
    public string? Note { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public string UpdatedBy { get; set; } = string.Empty;
    public bool IsDeleted { get; set; }
}

/// <summary>
/// 計算某位員工、某個月份的薪資。依當時該月份的出勤紀錄（AttendanceRecord）加總工時、算加班費，
/// 底薪直接抓 Employee.MonthlySalary 當下的值存成快照。如果該員工、該月份已經有一筆有效紀錄，
/// 會先把舊的軟刪除掉，再新增一筆（等於「重新計算」）。
/// </summary>
public class CalculatePayrollRequest
{
    [Required]
    public int EmployeeId { get; set; }

    /// <summary>只會看年/月，日期部分會自動忽略、視為當月 1 號。</summary>
    [Required]
    public DateTime PeriodMonth { get; set; }
}

/// <summary>幫某一筆薪資紀錄填入/修改獎金金額（人工輸入，沒有固定公式）。</summary>
public class UpdatePayrollBonusRequest
{
    [Range(0, double.MaxValue)]
    public decimal BonusAmount { get; set; }

    [StringLength(200)]
    public string? Note { get; set; }
}
