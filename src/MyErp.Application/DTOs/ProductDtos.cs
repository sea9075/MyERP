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

/// <summary>
/// 新增商品：Sku／Barcode 不開放使用者輸入，一律由 ProductService.CreateAsync 依分類編號
/// 自動產生（Sku＝分類編號 + "-" + 7 碼流水號，Barcode＝Sku 去掉 "-"），見該檔案的說明。
/// </summary>
public class CreateProductRequest
{
    [Required, StringLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>只在新增時可以選擇；建立後不能修改分類（見 UpdateProductRequest 的說明）。</summary>
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

/// <summary>
/// 修改商品：刻意不繼承 CreateProductRequest——編輯時不能換分類（分類決定商品標號的前綴，
/// 建立後就固定了，使用者需求明確表示「編輯不能修改分類，只能新增」），Sku/Barcode 也一樣
/// 是建立當下就固定、不能修改的欄位，所以這裡都不開放。
/// </summary>
public class UpdateProductRequest
{
    [Required, StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required, StringLength(10)]
    public string Unit { get; set; } = string.Empty;

    [Range(0, double.MaxValue)]
    public decimal CostPrice { get; set; }

    [Range(0, double.MaxValue)]
    public decimal SalePrice { get; set; }

    [Range(0, int.MaxValue)]
    public int SafetyStock { get; set; }

    public int? SupplierId { get; set; }

    /// <summary>
    /// 沿用 Phase 1 的做法，讓 Update 也能順便切換刪除狀態（例如取消刪除、恢復商品）。
    /// 一般刪除還是走 DELETE /api/products/{id}，不需要特別經過這裡。
    /// </summary>
    public bool IsDeleted { get; set; }
}
