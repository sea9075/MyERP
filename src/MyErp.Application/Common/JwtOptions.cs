namespace MyErp.Application.Common;

/// <summary>對應 appsettings.json 裡的 "Jwt" 區段。Key（簽章密鑰）務必只放在 user-secrets，不要進 Git。</summary>
public class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "MyErp";
    public string Audience { get; set; } = "MyErp";
    public string Key { get; set; } = string.Empty;
    public int ExpiresInMinutes { get; set; } = 480;
}
