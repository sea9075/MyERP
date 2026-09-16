namespace MyErp.Application.DTOs;

/// <summary>
/// 對應 GET /api/notifications（2026-09-15 新增：worker 低庫存自動通知，見 Infra-Progress.md §31）。
/// </summary>
public class NotificationDto
{
    public int Id { get; set; }

    /// <summary>目前只有 "LowStock" 一種。</summary>
    public string Type { get; set; } = string.Empty;

    public int ProductId { get; set; }
    public string? ProductName { get; set; }

    public string Message { get; set; } = string.Empty;

    public bool IsRead { get; set; }

    public DateTime CreatedAt { get; set; }
}
