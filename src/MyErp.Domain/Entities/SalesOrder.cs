using MyErp.Domain.Common;
using MyErp.Domain.Enums;

namespace MyErp.Domain.Entities;

/// <summary>
/// 出貨/銷售單（ERP.md §5.1 SalesOrder）。這張表已經有「作廢(Void)」機制當狀態管理，
/// 所以不套用 isDeleted，只套用 <see cref="ITrackable"/> 的 4 個稽核欄位（不含 IsDeleted）。
/// </summary>
public class SalesOrder : ITrackable
{
    public int Id { get; set; }

    /// <summary>單號，格式如 SO-20260908-001。</summary>
    public string OrderNo { get; set; } = string.Empty;

    /// <summary>客戶，可留空＝一般散客。</summary>
    public int? CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public DateTime OrderDate { get; set; }

    public OrderStatus Status { get; set; } = OrderStatus.Normal;

    public string? Note { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>建立者 username（原本是 CreatedByUserId 外鍵，這次改成直接存 username 字串快照）。</summary>
    public string CreatedBy { get; set; } = string.Empty;

    /// <summary>最後修改者 username；作廢(Void)也算一次異動，作廢時會更新這個欄位。</summary>
    public string UpdatedBy { get; set; } = string.Empty;

    public ICollection<SalesOrderItem> Items { get; set; } = new List<SalesOrderItem>();
}
