using System.ComponentModel.DataAnnotations;

namespace MyErp.Application.DTOs;

public class EmployeeDto
{
    public int Id { get; set; }
    public int UserId { get; set; }

    /// <summary>屬於 User，建立後不能修改，見 CreateEmployeeRequest 的說明。</summary>
    public string Username { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    /// <summary>"Product" / "HR" / "Manager" / "Admin"，跟 Department enum 對應。</summary>
    public string Department { get; set; } = string.Empty;

    public decimal MonthlySalary { get; set; }
    public DateTime HireDate { get; set; }
    public string? JobTitle { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? EmergencyContactName { get; set; }
    public string? EmergencyContactPhone { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public string UpdatedBy { get; set; } = string.Empty;

    /// <summary>true＝已離職（軟刪除）。</summary>
    public bool IsDeleted { get; set; }
}

/// <summary>
/// 新增員工＝同時開一組新的登入帳號（User）＋一筆人資資料（Employee），兩者一起建立。
/// Username 建立後就不能修改（User 的既有業務規則），所以只有在「新增」時才會出現。
/// </summary>
public class CreateEmployeeRequest
{
    [Required, StringLength(50)]
    public string Username { get; set; } = string.Empty;

    /// <summary>初始密碼，建立後直接可以登入；沒有「修改密碼」的 API，之後有需要再補（見交付說明）。</summary>
    [Required, StringLength(100, MinimumLength = 8, ErrorMessage = "密碼至少需要 8 個字元")]
    public string Password { get; set; } = string.Empty;

    [Required, StringLength(50)]
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>"Product" / "HR" / "Manager" / "Admin"（不分大小寫），同時決定這個帳號的權限。</summary>
    [Required]
    public string Department { get; set; } = string.Empty;

    [Range(0, double.MaxValue)]
    public decimal MonthlySalary { get; set; }

    [Required]
    public DateTime HireDate { get; set; }

    [StringLength(50)]
    public string? JobTitle { get; set; }

    [StringLength(30)]
    public string? Phone { get; set; }

    [StringLength(200)]
    public string? Address { get; set; }

    [StringLength(50)]
    public string? EmergencyContactName { get; set; }

    [StringLength(30)]
    public string? EmergencyContactPhone { get; set; }
}

/// <summary>
/// 修改員工：不含 Username/Password（帳號、密碼不透過這支 API 改，見上方說明）。
/// 沿用 Phase 1 對 Product/Supplier 的做法，讓 Update 也能順便切換刪除狀態（例如員工復職）。
/// </summary>
public class UpdateEmployeeRequest
{
    [Required, StringLength(50)]
    public string DisplayName { get; set; } = string.Empty;

    [Required]
    public string Department { get; set; } = string.Empty;

    [Range(0, double.MaxValue)]
    public decimal MonthlySalary { get; set; }

    [Required]
    public DateTime HireDate { get; set; }

    [StringLength(50)]
    public string? JobTitle { get; set; }

    [StringLength(30)]
    public string? Phone { get; set; }

    [StringLength(200)]
    public string? Address { get; set; }

    [StringLength(50)]
    public string? EmergencyContactName { get; set; }

    [StringLength(30)]
    public string? EmergencyContactPhone { get; set; }

    /// <summary>一般刪除（離職）還是走 DELETE /api/employees/{id}，不需要特別經過這裡。</summary>
    public bool IsDeleted { get; set; }
}

/// <summary>
/// HR/Manager/Admin 在員工管理裡幫員工重設密碼（2026-09-15 新增）。不需要輸入舊密碼——這是
/// 管理員層級的強制重設，跟使用者自己在右上角選單「密碼修改」（需要輸入目前密碼，見
/// AuthDtos.cs 的 ChangePasswordRequest）是兩支不同的 API，開放對象也不一樣。
/// </summary>
public class ResetEmployeePasswordRequest
{
    [Required, StringLength(100, MinimumLength = 8, ErrorMessage = "密碼至少需要 8 個字元")]
    public string NewPassword { get; set; } = string.Empty;
}
