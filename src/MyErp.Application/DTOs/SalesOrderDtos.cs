using System.ComponentModel.DataAnnotations;

namespace MyErp.Application.DTOs;

public class SalesOrderItemDto
{
    public int ProductId { get; set; }
    public string? ProductName { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Subtotal { get; set; }
}

public class SalesOrderDto
{
    public int Id { get; set; }
    public string OrderNo { get; set; } = string.Empty;
    public int? CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public DateTime OrderDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Note { get; set; }
    public decimal TotalAmount { get; set; }
    public List<SalesOrderItemDto> Items { get; set; } = [];

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public string UpdatedBy { get; set; } = string.Empty;
}

public class CreateSalesOrderItemRequest
{
    [Required]
    public int ProductId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "銷售數量必須大於 0")]
    public int Quantity { get; set; }

    [Range(0, double.MaxValue)]
    public decimal UnitPrice { get; set; }
}

public class CreateSalesOrderRequest
{
    /// <summary>可留空＝一般散客（ERP.md §4.4）。</summary>
    public int? CustomerId { get; set; }

    public DateTime? OrderDate { get; set; }

    [StringLength(200)]
    public string? Note { get; set; }

    [Required, MinLength(1, ErrorMessage = "出貨單至少需要一筆商品明細")]
    public List<CreateSalesOrderItemRequest> Items { get; set; } = [];
}
