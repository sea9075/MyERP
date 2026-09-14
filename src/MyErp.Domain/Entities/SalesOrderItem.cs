namespace MyErp.Domain.Entities;

/// <summary>銷售單明細（ERP.md §5.1 SalesOrderItem）。</summary>
public class SalesOrderItem
{
    public int Id { get; set; }

    public int SalesOrderId { get; set; }
    public SalesOrder? SalesOrder { get; set; }

    public int ProductId { get; set; }
    public Product? Product { get; set; }

    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Subtotal { get; set; }
}
