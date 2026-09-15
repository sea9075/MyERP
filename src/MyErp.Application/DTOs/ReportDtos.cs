namespace MyErp.Application.DTOs;

// 報表模組（ERP.md §4.6，Infra-Progress.md §27）。四種報表都只回傳彙總後的資料，
// 前端負責畫表格＋把目前畫面上的資料轉成 CSV 觸發瀏覽器下載（不走後端匯出端點，
// 這樣不用另外處理「下載連結要帶 JWT」的問題，見 §27 說明）。
//
// 共同的篩選規則：進貨/銷售統計、毛利報表一律排除已作廢（Voided）的單據，
// 不開放參數選擇要不要含作廢單——作廢單本來就代表「這筆交易沒有真的發生」。

// ===== 進貨統計 =====

public class PurchaseReportBySupplierDto
{
    public int SupplierId { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public int OrderCount { get; set; }
    public int TotalQuantity { get; set; }
    public decimal TotalAmount { get; set; }
}

public class PurchaseReportByProductDto
{
    public int ProductId { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string? CategoryName { get; set; }
    public int TotalQuantity { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal AverageUnitPrice { get; set; }
}

public class PurchaseReportOrderRowDto
{
    public int OrderId { get; set; }
    public string OrderNo { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; }
    public int SupplierId { get; set; }
    public string SupplierName { get; set; } = string.Empty;

    /// <summary>有帶商品/分類篩選時，這裡只加總「符合篩選條件」的明細，不是整張單的金額。</summary>
    public decimal TotalAmount { get; set; }
}

public class PurchaseReportDto
{
    public DateTime DateFrom { get; set; }
    public DateTime DateTo { get; set; }
    public int TotalOrderCount { get; set; }
    public int TotalQuantity { get; set; }
    public decimal TotalAmount { get; set; }
    public List<PurchaseReportBySupplierDto> BySupplier { get; set; } = [];
    public List<PurchaseReportByProductDto> ByProduct { get; set; } = [];
    public List<PurchaseReportOrderRowDto> Orders { get; set; } = [];
}

// ===== 銷售統計 =====

public class SalesReportByCustomerDto
{
    public int? CustomerId { get; set; }

    /// <summary>CustomerId 為 null 時固定顯示「一般散客」。</summary>
    public string CustomerName { get; set; } = string.Empty;
    public int OrderCount { get; set; }
    public decimal TotalAmount { get; set; }
}

public class SalesReportByProductDto
{
    public int ProductId { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string? CategoryName { get; set; }
    public int TotalQuantity { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal AverageUnitPrice { get; set; }
}

public class SalesReportOrderRowDto
{
    public int OrderId { get; set; }
    public string OrderNo { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; }
    public int? CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;

    /// <summary>有帶商品/分類篩選時，這裡只加總「符合篩選條件」的明細，不是整張單的金額。</summary>
    public decimal TotalAmount { get; set; }
}

public class SalesReportDto
{
    public DateTime DateFrom { get; set; }
    public DateTime DateTo { get; set; }
    public int TotalOrderCount { get; set; }
    public int TotalQuantity { get; set; }
    public decimal TotalAmount { get; set; }
    public List<SalesReportByCustomerDto> ByCustomer { get; set; } = [];
    public List<SalesReportByProductDto> ByProduct { get; set; } = [];
    public List<SalesReportOrderRowDto> Orders { get; set; } = [];
}

// ===== 毛利報表 =====

public class GrossMarginRowDto
{
    /// <summary>依分類彙總（GroupBy="category"）時為 null。</summary>
    public int? ProductId { get; set; }
    public string? Sku { get; set; }

    /// <summary>依商品彙總時是商品名稱；依分類彙總時是分類名稱。</summary>
    public string Name { get; set; } = string.Empty;
    public string? CategoryName { get; set; }
    public int QuantitySold { get; set; }
    public decimal SalesAmount { get; set; }

    /// <summary>
    /// 成本＝銷售數量 × 商品「目前」的參考成本價（Product.CostPrice），不是賣出當下的歷史成本
    /// （系統沒有做批次/歷史成本快照，見 Infra-Progress.md §27 的設計決策）。
    /// </summary>
    public decimal CostAmount { get; set; }
    public decimal GrossProfit { get; set; }

    /// <summary>0~100，SalesAmount 為 0 時固定回傳 0（避免除以 0）。</summary>
    public decimal GrossMarginPercent { get; set; }
}

public class GrossMarginReportDto
{
    public DateTime DateFrom { get; set; }
    public DateTime DateTo { get; set; }

    /// <summary>"product" 或 "category"。</summary>
    public string GroupBy { get; set; } = "product";
    public List<GrossMarginRowDto> Rows { get; set; } = [];
    public decimal TotalSalesAmount { get; set; }
    public decimal TotalCostAmount { get; set; }
    public decimal TotalGrossProfit { get; set; }
    public decimal OverallGrossMarginPercent { get; set; }
}

// ===== 庫存總覽/低庫存清單 =====

public class InventoryReportRowDto
{
    public int ProductId { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? CategoryName { get; set; }
    public string Unit { get; set; } = string.Empty;
    public int CurrentStock { get; set; }
    public int SafetyStock { get; set; }
    public bool IsLowStock { get; set; }
    public decimal CostPrice { get; set; }

    /// <summary>估計庫存金額 = CurrentStock × CostPrice。</summary>
    public decimal EstimatedValue { get; set; }
}

public class InventoryReportDto
{
    public List<InventoryReportRowDto> Rows { get; set; } = [];
    public int LowStockCount { get; set; }
    public decimal TotalEstimatedValue { get; set; }
}
