using System.ComponentModel.DataAnnotations;

namespace MyErp.Application.DTOs;

public class AttendanceRecordDto
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public string? EmployeeName { get; set; }
    public DateTime WorkDate { get; set; }
    public DateTime? ClockInAt { get; set; }
    public DateTime? ClockOutAt { get; set; }

    /// <summary>"SelfService" / "ManualEntry"，跟 AttendanceSource enum 對應。</summary>
    public string Source { get; set; } = string.Empty;

    /// <summary>ClockInAt/ClockOutAt 都有值時才會算，否則是 null（代表這天還沒打下班卡或資料不完整）。</summary>
    public decimal? WorkedHours { get; set; }

    public string? Note { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public string UpdatedBy { get; set; } = string.Empty;
    public bool IsDeleted { get; set; }
}

/// <summary>員工自己打「上班卡」：POST /api/attendance/clock-in，不用帶 EmployeeId，從登入帳號反查。</summary>
public class ClockInRequest
{
    [StringLength(200)]
    public string? Note { get; set; }
}

/// <summary>員工自己打「下班卡」：POST /api/attendance/clock-out，只會更新「今天」那一筆紀錄的 ClockOutAt。</summary>
public class ClockOutRequest
{
    [StringLength(200)]
    public string? Note { get; set; }
}

/// <summary>HR/Manager/Admin 幫員工手動建立/補登一筆出勤紀錄。</summary>
public class CreateManualAttendanceRequest
{
    [Required]
    public int EmployeeId { get; set; }

    [Required]
    public DateTime WorkDate { get; set; }

    public DateTime? ClockInAt { get; set; }
    public DateTime? ClockOutAt { get; set; }

    [StringLength(200)]
    public string? Note { get; set; }
}

/// <summary>HR/Manager/Admin 修改一筆出勤紀錄（自助打卡的紀錄也能改，例如忘記打下班卡由 HR 補上）。</summary>
public class UpdateManualAttendanceRequest
{
    [Required]
    public DateTime WorkDate { get; set; }

    public DateTime? ClockInAt { get; set; }
    public DateTime? ClockOutAt { get; set; }

    [StringLength(200)]
    public string? Note { get; set; }

    /// <summary>一般刪除還是走 DELETE，這裡只給「取消刪除」用。</summary>
    public bool IsDeleted { get; set; }
}
