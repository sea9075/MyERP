using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyErp.Application.DTOs;
using MyErp.Application.Services;

namespace MyErp.Api.Controllers;

/// <summary>
/// 報表模組（ERP.md §4.6，Infra-Progress.md §27）：進貨統計、銷售統計、毛利報表、庫存總覽/低庫存清單。
/// 依 2026-09-15 使用者決定，只開放 Manager/Admin 查看（跟 ActivityLogsController 同樣的權限收斂）。
/// CSV 匯出由前端把畫面上已經拿到的資料轉成 CSV 觸發下載，這裡不提供另外的匯出端點。
/// </summary>
[ApiController]
[Route("api/reports")]
[Authorize(Roles = "Manager,Admin")]
public class ReportsController(IReportService reportService) : ControllerBase
{
    /// <summary>GET /api/reports/purchases：進貨統計。dateFrom/dateTo 不帶時預設為本月。</summary>
    [HttpGet("purchases")]
    public async Task<ActionResult<PurchaseReportDto>> GetPurchaseReport(
        [FromQuery] DateTime? dateFrom,
        [FromQuery] DateTime? dateTo,
        [FromQuery] int? supplierId,
        [FromQuery] int? productId,
        [FromQuery] int? categoryId,
        CancellationToken ct)
    {
        var (from, to) = ResolveDateRange(dateFrom, dateTo);
        return Ok(await reportService.GetPurchaseReportAsync(from, to, supplierId, productId, categoryId, ct));
    }

    /// <summary>GET /api/reports/sales：銷售統計。dateFrom/dateTo 不帶時預設為本月。</summary>
    [HttpGet("sales")]
    public async Task<ActionResult<SalesReportDto>> GetSalesReport(
        [FromQuery] DateTime? dateFrom,
        [FromQuery] DateTime? dateTo,
        [FromQuery] int? customerId,
        [FromQuery] int? productId,
        [FromQuery] int? categoryId,
        CancellationToken ct)
    {
        var (from, to) = ResolveDateRange(dateFrom, dateTo);
        return Ok(await reportService.GetSalesReportAsync(from, to, customerId, productId, categoryId, ct));
    }

    /// <summary>
    /// GET /api/reports/gross-margin：毛利報表。dateFrom/dateTo 不帶時預設為本月。
    /// groupBy 預設 "product"，可傳 "category" 改成依分類彙總。
    /// 成本用商品「目前」的參考成本價估算，不是賣出當下的歷史成本，見 GrossMarginRowDto 註解。
    /// </summary>
    [HttpGet("gross-margin")]
    public async Task<ActionResult<GrossMarginReportDto>> GetGrossMarginReport(
        [FromQuery] DateTime? dateFrom,
        [FromQuery] DateTime? dateTo,
        [FromQuery] int? productId,
        [FromQuery] int? categoryId,
        [FromQuery] string groupBy = "product",
        CancellationToken ct = default)
    {
        var (from, to) = ResolveDateRange(dateFrom, dateTo);
        return Ok(await reportService.GetGrossMarginReportAsync(from, to, productId, categoryId, groupBy, ct));
    }

    /// <summary>GET /api/reports/inventory：庫存總覽/低庫存清單。lowStockOnly=true 時只回傳低於安全庫存的商品。</summary>
    [HttpGet("inventory")]
    public async Task<ActionResult<InventoryReportDto>> GetInventoryReport(
        [FromQuery] int? categoryId,
        [FromQuery] bool lowStockOnly,
        CancellationToken ct) =>
        Ok(await reportService.GetInventoryReportAsync(categoryId, lowStockOnly, ct));

    /// <summary>
    /// dateFrom/dateTo 沒帶時預設為「本月」（依伺服器 UTC 日期），dateTo 一律補到當天結束
    /// （23:59:59.9999999），這樣使用者傳「今天」當結束日期時，今天成立的單據才會被含進來。
    /// </summary>
    private static (DateTime From, DateTime To) ResolveDateRange(DateTime? dateFrom, DateTime? dateTo)
    {
        var today = DateTime.UtcNow.Date;
        var from = (dateFrom ?? new DateTime(today.Year, today.Month, 1)).Date;
        var toDate = (dateTo ?? from.AddMonths(1).AddDays(-1)).Date;
        var to = toDate.AddDays(1).AddTicks(-1);
        return (from, to);
    }
}
