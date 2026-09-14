using System.ComponentModel.DataAnnotations;

namespace MyErp.Application.DTOs;

public class LoginRequest
{
    [Required]
    public string Username { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}

public class LoginResponse
{
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; set; }
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>"Admin" 或 "Staff"，跟 UserRole enum 對應。</summary>
    public string Role { get; set; } = string.Empty;
}
