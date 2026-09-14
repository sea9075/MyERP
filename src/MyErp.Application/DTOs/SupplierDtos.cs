using System.ComponentModel.DataAnnotations;

namespace MyErp.Application.DTOs;

public class SupplierDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? ContactPerson { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? Note { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public string UpdatedBy { get; set; } = string.Empty;

    /// <summary>原本叫 IsActive，這次統一改名成 IsDeleted 並反轉語意（true＝已刪除）。</summary>
    public bool IsDeleted { get; set; }
}

public class CreateSupplierRequest
{
    [Required, StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [StringLength(50)]
    public string? ContactPerson { get; set; }

    [StringLength(30)]
    public string? Phone { get; set; }

    [StringLength(200)]
    public string? Address { get; set; }

    [StringLength(200)]
    public string? Note { get; set; }
}

public class UpdateSupplierRequest : CreateSupplierRequest
{
    /// <summary>
    /// 沿用 Phase 1 的做法，讓 Update 也能順便切換刪除狀態（例如取消刪除、恢復供應商）。
    /// 一般刪除還是走 DELETE /api/suppliers/{id}，不需要特別經過這裡。
    /// </summary>
    public bool IsDeleted { get; set; }
}
