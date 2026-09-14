namespace MyErp.Application.DTOs;

/// <summary>對應 GET /api/activity-logs：查詢「誰在什麼時候做了什麼」。</summary>
public class ActivityLogDto
{
    public int Id { get; set; }
    public string Api { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
}
