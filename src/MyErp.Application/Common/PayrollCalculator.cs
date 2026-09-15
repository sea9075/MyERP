namespace MyErp.Application.Common;

/// <summary>
/// 加班費試算的純計算邏輯，特地跟 PayrollService 分開放，方便寫單元測試（不需要 mock 任何
/// repository），也讓「規則長什麼樣子」一眼就看得到，不用在 Service 的一堆流程程式碼裡找。
///
/// 目前支援的規則（對應勞基法第 24 條，月薪制員工）：
/// - 時薪 = 月薪 ÷ 30 ÷ 8（勞動部公告的月薪制員工時薪換算方式）。
/// - 每天正常工時上限 8 小時；超過的部分視為加班。
/// - 加班前 2 小時：1.34 倍時薪。
/// - 加班超過 2 小時的部分：1.67 倍時薪。
///
/// 刻意不支援的部分（MVP 範圍，之後有需要再擴充）：例假日/休息日/國定假日出勤的特殊倍率、
/// 加班時數上限（勞基法規定平日加班一天原則上不能超過 4 小時，這裡不強制擋）、請假扣薪、
/// 遲到早退扣薪。這些都是「金額怎麼算」的規則，跟系統架構無關，之後單獨談規則就能加，
/// 不需要動到 Employee/AttendanceRecord/PayrollRecord 的資料結構。
/// </summary>
public static class PayrollCalculator
{
    public const decimal StandardDailyHours = 8m;
    public const decimal OvertimeTier1MaxHours = 2m;
    public const decimal OvertimeTier1Rate = 1.34m;
    public const decimal OvertimeTier2Rate = 1.67m;

    /// <summary>月薪 ÷ 30 ÷ 8。</summary>
    public static decimal CalculateHourlyRate(decimal monthlySalary) =>
        Math.Round(monthlySalary / 30m / StandardDailyHours, 2, MidpointRounding.AwayFromZero);

    /// <summary>
    /// 把「某一天實際工作的時數」拆成 正常工時 / 加班前2小時 / 加班超過2小時 三段。
    /// workedHours 為負值（例如打卡資料有誤，下班時間早於上班時間）會視為 0。
    /// </summary>
    public static (decimal regularHours, decimal overtimeTier1, decimal overtimeTier2) SplitDailyHours(decimal workedHours)
    {
        if (workedHours <= 0)
        {
            return (0m, 0m, 0m);
        }

        var regularHours = Math.Min(workedHours, StandardDailyHours);
        var overtimeHours = Math.Max(workedHours - StandardDailyHours, 0m);
        var tier1 = Math.Min(overtimeHours, OvertimeTier1MaxHours);
        var tier2 = Math.Max(overtimeHours - OvertimeTier1MaxHours, 0m);

        return (regularHours, tier1, tier2);
    }

    /// <summary>用時薪與兩段加班時數，算出加班費（四捨五入到小數點後 2 位）。</summary>
    public static decimal CalculateOvertimePay(decimal hourlyRate, decimal overtimeTier1, decimal overtimeTier2)
    {
        var pay = (hourlyRate * OvertimeTier1Rate * overtimeTier1) + (hourlyRate * OvertimeTier2Rate * overtimeTier2);
        return Math.Round(pay, 2, MidpointRounding.AwayFromZero);
    }
}
