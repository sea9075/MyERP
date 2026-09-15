using MyErp.Application.Abstractions;
using MyErp.Application.Common;
using MyErp.Application.DTOs;
using MyErp.Domain.Entities;
using MyErp.Domain.Enums;

namespace MyErp.Application.Services;

public interface IAttendanceService
{
    Task<List<AttendanceRecordDto>> SearchAsync(int? employeeId, DateTime? dateFrom, DateTime? dateTo, bool includeDeleted, CancellationToken ct = default);

    Task<AttendanceRecordDto> GetByIdAsync(int id, CancellationToken ct = default);

    /// <summary>員工查自己的出勤紀錄：currentUserId 反查 Employee 後再查，任何登入使用者都可以呼叫。</summary>
    Task<List<AttendanceRecordDto>> GetMyRecordsAsync(int currentUserId, DateTime? dateFrom, DateTime? dateTo, CancellationToken ct = default);

    /// <summary>員工自己打上班卡：currentUserId 是登入帳號的 User.Id，反查對應的 Employee。</summary>
    Task<AttendanceRecordDto> ClockInAsync(int currentUserId, ClockInRequest request, string currentUsername, CancellationToken ct = default);

    /// <summary>員工自己打下班卡：一定要先有今天的上班卡紀錄才能打下班卡。</summary>
    Task<AttendanceRecordDto> ClockOutAsync(int currentUserId, ClockOutRequest request, string currentUsername, CancellationToken ct = default);

    /// <summary>HR/Manager/Admin 幫員工手動建立/補登一筆出勤紀錄。</summary>
    Task<AttendanceRecordDto> CreateManualAsync(CreateManualAttendanceRequest request, string currentUsername, CancellationToken ct = default);

    /// <summary>HR/Manager/Admin 修改一筆出勤紀錄（不論原本是自助打卡還是手動建立的）。</summary>
    Task<AttendanceRecordDto> UpdateAsync(int id, UpdateManualAttendanceRequest request, string currentUsername, CancellationToken ct = default);

    /// <summary>軟刪除：HR 打錯資料要能刪掉重建。</summary>
    Task DeleteAsync(int id, string currentUsername, CancellationToken ct = default);
}

public class AttendanceService(
    IAttendanceRecordRepository attendanceRepository,
    IEmployeeRepository employeeRepository,
    IUnitOfWork unitOfWork)
    : IAttendanceService
{
    public async Task<List<AttendanceRecordDto>> SearchAsync(int? employeeId, DateTime? dateFrom, DateTime? dateTo, bool includeDeleted, CancellationToken ct = default)
    {
        var records = await attendanceRepository.SearchAsync(employeeId, dateFrom, dateTo, includeDeleted, ct);
        return records.Select(ToDto).ToList();
    }

    public async Task<AttendanceRecordDto> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var record = await attendanceRepository.GetByIdAsync(id, ct)
            ?? throw new BusinessRuleException($"找不到出勤紀錄 (Id={id})。");
        return ToDto(record);
    }

    public async Task<List<AttendanceRecordDto>> GetMyRecordsAsync(int currentUserId, DateTime? dateFrom, DateTime? dateTo, CancellationToken ct = default)
    {
        var employee = await GetEmployeeByUserIdAsync(currentUserId, ct);
        var records = await attendanceRepository.SearchAsync(employee.Id, dateFrom, dateTo, includeDeleted: false, ct);
        return records.Select(ToDto).ToList();
    }

    public async Task<AttendanceRecordDto> ClockInAsync(int currentUserId, ClockInRequest request, string currentUsername, CancellationToken ct = default)
    {
        var employee = await GetEmployeeByUserIdAsync(currentUserId, ct);
        var today = DateTime.UtcNow.Date;

        var existing = await attendanceRepository.GetByEmployeeAndDateAsync(employee.Id, today, ct);
        if (existing is { ClockInAt: not null })
        {
            throw new BusinessRuleException("今天已經打過上班卡了。");
        }

        if (existing is not null)
        {
            // 理論上很少見（例如 HR 先幫忙補了一筆只有下班時間的紀錄），把上班時間補上去就好，不用建新的一筆。
            existing.ClockInAt = DateTime.UtcNow;
            existing.Note = request.Note.TrimOrNull() ?? existing.Note;
            existing.TouchUpdated(currentUsername);
            await unitOfWork.SaveChangesAsync(ct);
            return ToDto(existing);
        }

        var record = new AttendanceRecord
        {
            EmployeeId = employee.Id,
            WorkDate = today,
            ClockInAt = DateTime.UtcNow,
            Source = AttendanceSource.SelfService,
            Note = request.Note.TrimOrNull(),
        };
        record.InitializeAudit(currentUsername);
        attendanceRepository.Add(record);
        await unitOfWork.SaveChangesAsync(ct);
        return ToDto(record);
    }

    public async Task<AttendanceRecordDto> ClockOutAsync(int currentUserId, ClockOutRequest request, string currentUsername, CancellationToken ct = default)
    {
        var employee = await GetEmployeeByUserIdAsync(currentUserId, ct);
        var today = DateTime.UtcNow.Date;

        var existing = await attendanceRepository.GetByEmployeeAndDateAsync(employee.Id, today, ct)
            ?? throw new BusinessRuleException("今天還沒有打上班卡，無法打下班卡。");

        existing.ClockOutAt = DateTime.UtcNow;
        if (request.Note.TrimOrNull() is { } note)
        {
            existing.Note = note;
        }
        existing.TouchUpdated(currentUsername);

        await unitOfWork.SaveChangesAsync(ct);
        return ToDto(existing);
    }

    public async Task<AttendanceRecordDto> CreateManualAsync(CreateManualAttendanceRequest request, string currentUsername, CancellationToken ct = default)
    {
        var employee = await employeeRepository.GetByIdAsync(request.EmployeeId, ct)
            ?? throw new BusinessRuleException($"找不到員工 (Id={request.EmployeeId})。");

        var workDate = request.WorkDate.Date;
        var existing = await attendanceRepository.GetByEmployeeAndDateAsync(employee.Id, workDate, ct);
        if (existing is not null)
        {
            throw new BusinessRuleException($"{employee.User?.DisplayName} 在 {workDate:yyyy-MM-dd} 已經有一筆出勤紀錄了，請改用修改功能。");
        }

        var record = new AttendanceRecord
        {
            EmployeeId = employee.Id,
            WorkDate = workDate,
            ClockInAt = request.ClockInAt,
            ClockOutAt = request.ClockOutAt,
            Source = AttendanceSource.ManualEntry,
            Note = request.Note.TrimOrNull(),
        };
        record.InitializeAudit(currentUsername);
        attendanceRepository.Add(record);
        await unitOfWork.SaveChangesAsync(ct);
        return ToDto(record);
    }

    public async Task<AttendanceRecordDto> UpdateAsync(int id, UpdateManualAttendanceRequest request, string currentUsername, CancellationToken ct = default)
    {
        var record = await attendanceRepository.GetByIdAsync(id, ct)
            ?? throw new BusinessRuleException($"找不到出勤紀錄 (Id={id})。");

        var workDate = request.WorkDate.Date;
        if (workDate != record.WorkDate)
        {
            var conflict = await attendanceRepository.GetByEmployeeAndDateAsync(record.EmployeeId, workDate, ct);
            if (conflict is not null && conflict.Id != record.Id)
            {
                throw new BusinessRuleException($"這位員工在 {workDate:yyyy-MM-dd} 已經有另一筆出勤紀錄了。");
            }
        }

        record.WorkDate = workDate;
        record.ClockInAt = request.ClockInAt;
        record.ClockOutAt = request.ClockOutAt;
        record.Note = request.Note.TrimOrNull();
        record.IsDeleted = request.IsDeleted;
        record.TouchUpdated(currentUsername);

        await unitOfWork.SaveChangesAsync(ct);
        return ToDto(record);
    }

    public async Task DeleteAsync(int id, string currentUsername, CancellationToken ct = default)
    {
        var record = await attendanceRepository.GetByIdAsync(id, ct)
            ?? throw new BusinessRuleException($"找不到出勤紀錄 (Id={id})。");

        record.SoftDelete(currentUsername);
        await unitOfWork.SaveChangesAsync(ct);
    }

    private async Task<Employee> GetEmployeeByUserIdAsync(int userId, CancellationToken ct)
    {
        return await employeeRepository.GetByUserIdAsync(userId, ct)
            ?? throw new BusinessRuleException("找不到你的員工資料，無法打卡，請聯絡人資建立員工資料。");
    }

    private static AttendanceRecordDto ToDto(AttendanceRecord record) => new()
    {
        Id = record.Id,
        EmployeeId = record.EmployeeId,
        EmployeeName = record.Employee?.User?.DisplayName,
        WorkDate = record.WorkDate,
        ClockInAt = record.ClockInAt,
        ClockOutAt = record.ClockOutAt,
        Source = record.Source.ToString(),
        WorkedHours = record is { ClockInAt: not null, ClockOutAt: not null }
            ? Math.Round((decimal)(record.ClockOutAt!.Value - record.ClockInAt!.Value).TotalHours, 2, MidpointRounding.AwayFromZero)
            : null,
        Note = record.Note,
        CreatedAt = record.CreatedAt,
        UpdatedAt = record.UpdatedAt,
        CreatedBy = record.CreatedBy,
        UpdatedBy = record.UpdatedBy,
        IsDeleted = record.IsDeleted,
    };
}
