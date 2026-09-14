using System.ComponentModel.DataAnnotations;

namespace MyErp.Application.DTOs;

public class ProductDto
{
    public int Id { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string? Barcode { get; set; }
    public string Name { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public string Unit { get; set; } = string.Empty;
    public decimal CostPrice { get; set; }
    public decimal SalePrice { get; set; }
    public int SafetyStock { get; set; }
    public int CurrentStock { get; set; }
    public bool IsLowStock => CurrentStock < SafetyStock;
    public int? SupplierId { get; set; }
    public string? SupplierName { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public string UpdatedBy { get; set; } = string.Empty;

    /// <summary>原本叫 IsActive，這次統一改名成 IsDeleted 並反轉語意（true＝已刪除）。</summary>
    public bool IsDeleted { get; set; }
}

public class CreateProductRequest
{
    [Required, StringLength(30)]
    public string Sku { get; set; } = string.Empty;

    [StringLength(30)]
    public string? Barcode { get; set; }

    [Required, StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    public int CategoryId { get; set; }

    [Required, StringLength(10)]
    public string Unit { get; set; } = string.Empty;

    [Range(0, double.MaxValue)]
    public decimal CostPrice { get; set; }

    [Range(0, double.MaxValue)]
    public decimal SalePrice { get; set; }

    [Range(0, int.MaxValue)]
    public int SafetyStock { get; set; }

    public int? SupplierId { get; set; }
}

public class UpdateProductRequest : CreateProductRequest
{
    /// <summary>
    /// 沿用 Phase 1 的做法，讓 Update 也能順便切換刪除狀態（例如取消刪除、恢復商品）。
    /// 一般刪除還是走 DELETE /api/products/{id}，不需要特別經過這裡。
    /// </summary>
    public bool IsDeleted { get; set; }
}
