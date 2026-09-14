using System.ComponentModel.DataAnnotations;

namespace MyErp.Application.DTOs;

public class PurchaseOrderItemDto
{
    public int ProductId { get; set; }
    public string? ProductName { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Subtotal { get; set; }
}

public class PurchaseOrderDto
{
    public int Id { get; set; }
    public string OrderNo { get; set; } = string.Empty;
    public int SupplierId { get; set; }
    public string? SupplierName { get; set; }
    public DateTime OrderDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Note { get; set; }
    public decimal TotalAmount { get; set; }
    public List<PurchaseOrderItemDto> Items { get; set; } = [];
}

public class CreatePurchaseOrderItemRequest
{
    [Required]
    public int ProductId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "進貨數量必須大於 0")]
    public int Quantity { get; set; }

    [Range(0, double.MaxValue)]
    public decimal UnitPrice { get; set; }
}

public class CreatePurchaseOrderRequest
{
    [Required]
    public int SupplierId { get; set; }

    public DateTime? OrderDate { get; set; }

    [StringLength(200)]
    public string? Note { get; set; }

    [Required, MinLength(1, ErrorMessage = "進貨單至少需要一筆商品明細")]
    public List<CreatePurchaseOrderItemRequest> Items { get; set; } = [];
}
