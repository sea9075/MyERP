using MyErp.Application.Common;
using Xunit;

namespace MyErp.Tests.Common;

public class PasswordHasherTests
{
    [Fact]
    public void Verify_應該在密碼正確時回傳true()
    {
        var hash = PasswordHasher.Hash("Admin@123456");

        var result = PasswordHasher.Verify("Admin@123456", hash);

        Assert.True(result);
    }

    [Fact]
    public void Verify_應該在密碼錯誤時回傳false()
    {
        var hash = PasswordHasher.Hash("Admin@123456");

        var result = PasswordHasher.Verify("WrongPassword", hash);

        Assert.False(result);
    }

    [Fact]
    public void Hash_同一組密碼每次雜湊結果都應該不同_因為有隨機salt()
    {
        var hash1 = PasswordHasher.Hash("Admin@123456");
        var hash2 = PasswordHasher.Hash("Admin@123456");

        Assert.NotEqual(hash1, hash2);

        // 但兩個雜湊值都應該還是能驗證回同一組明文密碼。
        Assert.True(PasswordHasher.Verify("Admin@123456", hash1));
        Assert.True(PasswordHasher.Verify("Admin@123456", hash2));
    }
}
