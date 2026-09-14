using System.Security.Cryptography;

namespace MyErp.Application.Common;

/// <summary>
/// 輕量的密碼雜湊工具（PBKDF2 + 隨機 Salt），不依賴任何額外套件
/// （沒有引入完整的 ASP.NET Core Identity），對應 ERP.md §2「自建 Users 表」的選擇。
/// 儲存格式："{iterations}.{base64(salt)}.{base64(hash)}"，
/// 把 iteration 次數也存起來，未來想調高安全性時舊密碼還是能驗證。
/// </summary>
public static class PasswordHasher
{
    private const int SaltSize = 16;       // 128 bit
    private const int HashSize = 32;       // 256 bit
    private const int Iterations = 100_000;

    public static string Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, HashSize);
        return $"{Iterations}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
    }

    public static bool Verify(string password, string hashedValue)
    {
        var parts = hashedValue.Split('.', 3);
        if (parts.Length != 3)
        {
            return false;
        }

        if (!int.TryParse(parts[0], out var iterations))
        {
            return false;
        }

        var salt = Convert.FromBase64String(parts[1]);
        var expectedHash = Convert.FromBase64String(parts[2]);

        var actualHash = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, expectedHash.Length);
        return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
    }
}
