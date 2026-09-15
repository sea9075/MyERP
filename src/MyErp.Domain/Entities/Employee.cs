using MyErp.Domain.Common;

namespace MyErp.Domain.Entities;

/// <summary>
/// 員工人資資料（新增：人資系統）。
///
/// 刻意跟 User（登入帳號、密碼、部門/權限）分成兩張表，用 UserId 一對一關聯：User 負責「能不能登入、
/// 能做什麼」，Employee 負責「薪水、到職日期」這些人資資訊，兩者關注點不同，之後如果要讓某些人
/// 只有登入帳號、沒有人資資料（或反過來），也還有彈性。
///
/// UserId 是必填、唯一的外鍵：因為員工自助打卡（見 AttendanceRecord）一定要先登入系統，所以「建立
/// 員工」這個動作在 EmployeeService 裡會同時建立一組新的登入帳號，兩者一起產生、一起存在。
///
/// Username 屬於 User，不屬於 Employee，而 Username 本來就不能修改（User 類別的既有業務規則），
/// 所以「員工帳號不能改」這件事不需要在 Employee 這邊另外處理。
/// </summary>
public class Employee : IAuditable
{
    public int Id { get; set; }

    public int UserId { get; set; }
    public User? User { get; set; }

    /// <summary>月薪（底薪）。加班費、獎金另外算，存在 PayrollRecord，不會動到這個欄位。</summary>
    public decimal MonthlySalary { get; set; }

    public DateTime HireDate { get; set; }

    public string? JobTitle { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? EmergencyContactName { get; set; }
    public string? EmergencyContactPhone { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public string CreatedBy { get; set; } = string.Empty;
    public string UpdatedBy { get; set; } = string.Empty;

    /// <summary>是否已刪除（軟刪除＝離職）。離職日期直接看 UpdatedAt，沿用既有的軟刪除慣例。</summary>
    public bool IsDeleted { get; set; }

    public ICollection<AttendanceRecord> AttendanceRecords { get; set; } = new List<AttendanceRecord>();
    public ICollection<PayrollRecord> PayrollRecords { get; set; } = new List<PayrollRecord>();
}
