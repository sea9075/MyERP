using MyErp.Application.Abstractions;
using MyErp.Application.Common;
using MyErp.Application.DTOs;
using MyErp.Domain.Entities;

namespace MyErp.Application.Services;

public interface IPayrollService
{
    Task<List<PayrollRecordDto>> SearchAsync(int? employeeId, DateTime? periodMonth, bool includeDeleted, CancellationToken ct = default);
    Task<PayrollRecordDto> GetByIdAsync(int id, CancellationToken ct = default);

    /// <summary>依當月出勤紀錄計算薪資（含加班費）。如果該員工這個月已經算過，會先把舊的軟刪除掉再重算。</summary>
    Task<PayrollRecordDto> CalculateAsync(CalculatePayrollRequest request, string currentUsername, CancellationToken ct = default);

    /// <summary>填入/修改獎金金額（人工輸入），同時重新算 TotalPay。</summary>
    Task<PayrollRecordDto> UpdateBonusAsync(int id, UpdatePayrollBonusRequest request, string currentUsername, CancellationToken ct = default);

    Task DeleteAsync(int id, string currentUsername, CancellationToken ct = default);
}

public class PayrollService(
    IPayrollRecordRepository payrollRepository,
    IEmployeeRepository employeeRepository,
    IAttendanceRecordRepository attendanceRepository,
    IUnitOfWork unitOfWork)
    : IPayrollService
{
    public async Task<List<PayrollRecordDto>> SearchAsync(int? employeeId, DateTime? periodMonth, bool includeDeleted, CancellationToken ct = default)
    {
        var records = await payrollRepository.SearchAsync(employeeId, periodMonth, includeDeleted, ct);
        return records.Select(ToDto).ToList();
    }

    public async Task<PayrollRecordDto> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var record = await payrollRepository.GetByIdAsync(id, ct)
            ?? throw new BusinessRuleException($"找不到薪資紀錄 (Id={id})。");
        return ToDto(record);
    }

    public async Task<PayrollRecordDto> CalculateAsync(CalculatePayrollRequest request, string currentUsername, CancellationToken ct = default)
    {
        var employee = await employeeRepository.GetByIdAsync(request.EmployeeId, ct)
            ?? throw new BusinessRuleException($"找不到員工 (Id={request.EmployeeId})。");

        var periodMonth = new DateTime(request.PeriodMonth.Year, request.PeriodMonth.Month, 1);
        var periodEnd = periodMonth.AddMonths(1).AddDays(-1);

        // 重新計算：先把這個員工、這個月份原本那筆（如果有）軟刪除掉，獎金/備註沿用舊的，
        // 因為獎金是人工輸入的東西，不應該因為「重算工時」就被清空。
        var existing = await payrollRepository.GetByEmployeeAndMonthAsync(employee.Id, periodMonth, ct);
        var carriedBonus = existing?.BonusAmount ?? 0m;
        var carriedNote = existing?.Note;
        existing?.SoftDelete(currentUsername);

        var attendanceRecords = await attendanceRepository.SearchAsync(employee.Id, periodMonth, periodEnd, includeDeleted: false, ct);

        decimal regularTotal = 0m;
        decimal tier1Total = 0m;
        decimal tier2Total = 0m;

        foreach (var attendance in attendanceRecords)
        {
            if (attendance.ClockInAt is null || attendance.ClockOutAt is null)
            {
                // 只打了上班卡沒打下班卡（或反過來，理論上不會發生）的紀錄，工時算不出來，這裡先跳過，
                // 不計入這個月的工時——HR 應該先把這筆出勤紀錄補完整，再重新計算薪資。
                continue;
            }

            var workedHours = (decimal)(attendance.ClockOutAt.Value - attendance.ClockInAt.Value).TotalHours;
            var (regular, tier1, tier2) = PayrollCalculator.SplitDailyHours(workedHours);
            regularTotal += regular;
            tier1Total += tier1;
            tier2Total += tier2;
        }

        var hourlyRate = PayrollCalculator.CalculateHourlyRate(employee.MonthlySalary);
        var overtimePay = PayrollCalculator.CalculateOvertimePay(hourlyRate, tier1Total, tier2Total);
        var totalPay = employee.MonthlySalary + overtimePay + carriedBonus;

        var record = new PayrollRecord
        {
            EmployeeId = employee.Id,
            PeriodMonth = periodMonth,
            BaseSalary = employee.MonthlySalary,
            RegularHours = regularTotal,
            OvertimeHoursTier1 = tier1Total,
            OvertimeHoursTier2 = tier2Total,
            OvertimePay = overtimePay,
            BonusAmount = carriedBonus,
            TotalPay = totalPay,
            Note = carriedNote,
        };
        record.InitializeAudit(currentUsername);
        payrollRepository.Add(record);

        await unitOfWork.SaveChangesAsync(ct);

        record.Employee = employee;
        return ToDto(record);
    }

    public async Task<PayrollRecordDto> UpdateBonusAsync(int id, UpdatePayrollBonusRequest request, string currentUsername, CancellationToken ct = default)
    {
        var record = await payrollRepository.GetByIdAsync(id, ct)
            ?? throw new BusinessRuleException($"找不到薪資紀錄 (Id={id})。");

        record.BonusAmount = request.BonusAmount;
        record.Note = request.Note.TrimOrNull();
        record.TotalPay = record.BaseSalary + record.OvertimePay + record.BonusAmount;
        record.TouchUpdated(currentUsername);

        await unitOfWork.SaveChangesAsync(ct);
        return ToDto(record);
    }

    public async Task DeleteAsync(int id, string currentUsername, CancellationToken ct = default)
    {
        var record = await payrollRepository.GetByIdAsync(id, ct)
            ?? throw new BusinessRuleException($"找不到薪資紀錄 (Id={id})。");

        record.SoftDelete(currentUsername);
        await unitOfWork.SaveChangesAsync(ct);
    }

    private static PayrollRecordDto ToDto(PayrollRecord record) => new()
    {
        Id = record.Id,
        EmployeeId = record.EmployeeId,
        EmployeeName = record.Employee?.User?.DisplayName,
        PeriodMonth = record.PeriodMonth,
        BaseSalary = record.BaseSalary,
        RegularHours = record.RegularHours,
        OvertimeHoursTier1 = record.OvertimeHoursTier1,
        OvertimeHoursTier2 = record.OvertimeHoursTier2,
        OvertimePay = record.OvertimePay,
        BonusAmount = record.BonusAmount,
        TotalPay = record.TotalPay,
        Note = record.Note,
        CreatedAt = record.CreatedAt,
        UpdatedAt = record.UpdatedAt,
        CreatedBy = record.CreatedBy,
        UpdatedBy = record.UpdatedBy,
        IsDeleted = record.IsDeleted,
    };
}
