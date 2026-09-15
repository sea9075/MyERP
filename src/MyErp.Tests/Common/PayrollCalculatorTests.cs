using MyErp.Application.Common;
using Xunit;

namespace MyErp.Tests.Common;

public class PayrollCalculatorTests
{
    [Fact]
    public void CalculateHourlyRate_月薪36000應該等於時薪150()
    {
        // 36000 / 30 / 8 = 150
        var rate = PayrollCalculator.CalculateHourlyRate(36000m);

        Assert.Equal(150m, rate);
    }

    [Fact]
    public void SplitDailyHours_工作8小時應該全部算正常工時_沒有加班()
    {
        var (regular, tier1, tier2) = PayrollCalculator.SplitDailyHours(8m);

        Assert.Equal(8m, regular);
        Assert.Equal(0m, tier1);
        Assert.Equal(0m, tier2);
    }

    [Fact]
    public void SplitDailyHours_工作9小時應該有1小時算Tier1加班()
    {
        var (regular, tier1, tier2) = PayrollCalculator.SplitDailyHours(9m);

        Assert.Equal(8m, regular);
        Assert.Equal(1m, tier1);
        Assert.Equal(0m, tier2);
    }

    [Fact]
    public void SplitDailyHours_工作11小時應該Tier1滿2小時_Tier2有1小時()
    {
        var (regular, tier1, tier2) = PayrollCalculator.SplitDailyHours(11m);

        Assert.Equal(8m, regular);
        Assert.Equal(2m, tier1);
        Assert.Equal(1m, tier2);
    }

    [Fact]
    public void SplitDailyHours_工時為0或負值應該全部回傳0()
    {
        var zero = PayrollCalculator.SplitDailyHours(0m);
        var negative = PayrollCalculator.SplitDailyHours(-1m);

        Assert.Equal((0m, 0m, 0m), zero);
        Assert.Equal((0m, 0m, 0m), negative);
    }

    [Fact]
    public void CalculateOvertimePay_時薪150_Tier1兩小時_Tier2一小時()
    {
        // 150 * 1.34 * 2 + 150 * 1.67 * 1 = 402 + 250.5 = 652.5
        var pay = PayrollCalculator.CalculateOvertimePay(150m, 2m, 1m);

        Assert.Equal(652.5m, pay);
    }

    [Fact]
    public void CalculateOvertimePay_沒有加班時數應該是0元()
    {
        var pay = PayrollCalculator.CalculateOvertimePay(150m, 0m, 0m);

        Assert.Equal(0m, pay);
    }
}
