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
    public bool IsActive { get; set; }
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
    public bool IsActive { get; set; } = true;
}
